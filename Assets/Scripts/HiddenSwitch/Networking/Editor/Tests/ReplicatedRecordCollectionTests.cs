using System.Collections.Generic;
using System.Linq;
using HiddenSwitch.Networking.Peers.Internal;
using NUnit.Framework;

namespace HiddenSwitch.Networking.Editor.Tests
{
    [TestFixture]
    public class ReplicatedRecordCollectionTests
    {
        public struct SpecialRecord : IId
        {
            public int id { get; set; }
            public int value { get; set; }
        }

        [Test]
        public void TestBasicSet()
        {
            var coll = new ReplicatedReactiveRecordCollection<SpecialRecord>("test");
            var record = coll.CreateRecord();
            record.value = 10;
            coll.Add(record);
            Assert.AreEqual(10, coll[0].value);
            var record2 = record;
            record2.value = 20;
            coll[0] = record2;
            Assert.AreEqual(20, coll[0].value);
            Assert.AreEqual(10, record.value);
            Assert.AreEqual(record2.id, record.id);
            Assert.AreEqual(record.id, coll[0].id);
            Assert.AreEqual(record2.id, coll[0].id);
            Assert.AreEqual(coll.Count, 1);
            foreach (var rec in coll)
            {
                // Only one!
                Assert.AreEqual(20, record2.value);
            }
        }

        [Test]
        public void TestForeignSetInOrder()
        {
            var coll = new ReplicatedReactiveRecordCollection<SpecialRecord>("local");
            var ops = new List<KSEQOperation<SpecialRecord>>();
            var record1 = coll.CreateRecord();
            record1.value = 10;
            coll.Add(record1);
            ops.Add(coll.lastOp.Value);
            var record2 = coll.CreateRecord();
            record2.value = 20;
            coll.Add(record2);
            ops.Add(coll.lastOp.Value);
            var record3 = record1;
            record3.value = 30;
            coll[0] = record3;
            ops.Add(coll.lastOp.Value);

            var coll2 = new ReplicatedReactiveRecordCollection<SpecialRecord>("foreign");
            // Apply in order
            foreach (var op in ops)
            {
                coll2.Apply(op);
            }

            Assert.IsTrue(coll.SequenceEqual(coll2));
        }

        [Test]
        public void TestForeignSetNotInOrder()
        {
            var coll = new ReplicatedReactiveRecordCollection<SpecialRecord>("local");
            var ops = new List<KSEQOperation<SpecialRecord>>();
            var record1 = coll.CreateRecord();
            record1.value = 10;
            coll.Add(record1);
            ops.Add(coll.lastOp.Value);
            var record2 = coll.CreateRecord();
            record2.value = 20;
            coll.Add(record2);
            ops.Add(coll.lastOp.Value);
            var record3 = record1;
            record3.value = 30;
            coll[0] = record3;
            ops.Add(coll.lastOp.Value);

            var coll2 = new ReplicatedReactiveRecordCollection<SpecialRecord>("foreign");
            // Apply set first
            coll2.Apply(ops[2]);
            coll2.Apply(ops[1]);
            coll2.Apply(ops[0]);

            Assert.IsTrue(coll.SequenceEqual(coll2));
        }

        [Test]
        public void TestReplaceOtherReplicaRecord()
        {
            var coll1 = new ReplicatedReactiveRecordCollection<SpecialRecord>("local");
            var coll2 = new ReplicatedReactiveRecordCollection<SpecialRecord>("foreign");
            var rec1 = coll1.CreateRecord();
            rec1.value = 10;
            coll1.Add(rec1);
            var op1 = coll1.lastOp.Value;
            coll2.Apply(op1);
            var rec2 = coll2[0];
            rec2.value = 20;
            coll2.Replace(rec2);
            var op2 = coll2.lastOp.Value;
            coll1.Apply(op2);
            Assert.IsTrue(coll1.SequenceEqual(coll2));
        }

        [Test]
        public void TestRandomMultiReplica()
        {
            var random = new System.Random();
            var alice = new ReplicatedReactiveRecordCollection<SpecialRecord>("alice");
            var bob = new ReplicatedReactiveRecordCollection<SpecialRecord>("bob");

            KSEQOperation<SpecialRecord>?[] GenerateOps(ReplicatedReactiveRecordCollection<SpecialRecord> replica)
            {
                return Enumerable.Range(0, 1000).Select(i =>
                    {
                        var rec = replica.CreateRecord();
                        rec.value = i;
                        replica.Insert(random.Next(i), rec);
                        return replica.lastOp;
                    }).Concat(Enumerable.Range(0, 100)
                        .Select(i =>
                        {
                            replica.RemoveAt(random.Next(1000 - i));
                            return replica.lastOp;
                        }))
                    .Concat(Enumerable.Range(0, 900)
                        .Select(i =>
                        {
                            var index = random.Next(i);
                            var replacement = new SpecialRecord()
                            {
                                id = replica[index].id,
                                value = i
                            };
                            replica.Replace(replacement);
                            return replica.lastOp;
                        })).ToArray();
            }

            var aliceOps = GenerateOps(alice);
            var bobOps = GenerateOps(bob);

            KSEQTests.Shuffle(aliceOps, random);
            KSEQTests.Shuffle(bobOps, random);

            foreach (var op in aliceOps)
            {
                if (op != null)
                {
                    bob.Apply(op.Value);
                }
            }

            foreach (var op in bobOps)
            {
                if (op != null)
                {
                    alice.Apply(op.Value);
                }
            }


            Assert.AreEqual(alice.Count, bob.Count);
            Assert.IsTrue(alice.ToArray().SequenceEqual(bob.ToArray()));
        }
    }
}