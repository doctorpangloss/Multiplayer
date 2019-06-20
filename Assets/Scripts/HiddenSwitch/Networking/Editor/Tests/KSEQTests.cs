using System;
using System.Collections.Generic;
using System.Linq;
using HiddenSwitch.Networking.Peers.Internal;
using NUnit.Framework;

namespace HiddenSwitch.Networking.Editor.Tests
{
    [TestFixture]
    public class KSEQTests
    {
        [Test]
        public void TestConstructor()
        {
            var seq = new KSEQReplicatedList<string>("test");
            Assert.IsNotNull(seq);
        }

        [Test]
        public void TestReplicaAssigned()
        {
            var name = "randomname";
            var seq = new KSEQReplicatedList<string>(name);
            Assert.AreEqual(name, seq.replicaId);
        }

        [Test]
        public void TestRangeErrorInvalidIndex()
        {
            var seq = new KSEQReplicatedList<string>("test");
            Assert.Throws<ArgumentOutOfRangeException>(() => { seq.Insert(-1, "a"); });
        }

        [Test]
        public void TestValidInsertOp()
        {
            var seq = new KSEQReplicatedList<string>("test");
            seq.Insert(0, "a");
            var op = seq.lastOp;
            Assert.AreEqual(KSEQOperationTypes.Insert, op?.op);
            Assert.AreEqual("a", op?.value);
        }

        [Test]
        public void TestAddAtomToEndOfSequence()
        {
            var seq = new KSEQReplicatedList<string>("test");
            seq.Insert(0, "a");
            seq.Insert(1, "b");
            Assert.AreEqual(2, seq.Count);
            Assert.AreEqual("a", seq[0]);
            Assert.AreEqual("b", seq[1]);
        }

        [Test]
        public void TestAtomToBeginningOfSequence()
        {
            var seq = new KSEQReplicatedList<string>("test");
            seq.Insert(0, "a");
            seq.Insert(1, "b");
            seq.Insert(0, "c");
            Assert.AreEqual(seq.Count, 3);
            Assert.AreEqual(seq[0], "c");
            Assert.AreEqual(seq[1], "a");
            Assert.AreEqual(seq[2], "b");
        }

        [Test]
        public void TestAdd1000ItemsToEnd()
        {
            var seq = new KSEQReplicatedList<int>("test");
            for (var i = 0; i < 1000; i++)
            {
                seq.Add(i);
            }

            Assert.AreEqual(1000, seq.Count);
            Assert.AreEqual(999, seq[seq.Count - 1]);
        }

        [Test]
        public void TestAdd1000ItemsToBeginning()
        {
            var seq = new KSEQReplicatedList<int>("test");
            for (var i = 0; i < 1000; i++)
            {
                seq.Insert(0, i);
            }

            Assert.AreEqual(1000, seq.Count);
            Assert.AreEqual(0, seq[seq.Count - 1]);
            Assert.AreEqual(999, seq[0]);
        }

        [Test]
        public void TetsRemoveNegativePositionThrows()
        {
            var seq = new KSEQReplicatedList<string>("test");
            Assert.Throws<ArgumentOutOfRangeException>(() => { seq.RemoveAt(-1); });
        }

        [Test]
        public void TestPositionOutOfRangeDoesNotThrow()
        {
            var seq = new KSEQReplicatedList<string>("test");
            Assert.DoesNotThrow(() => { seq.RemoveAt(100); });
            Assert.IsNull(seq.lastOp);
        }

        [Test]
        public void TestRemoveAtom()
        {
            var seq = new KSEQReplicatedList<string>("test");
            seq.Add("a");
            Assert.AreEqual("a", seq[0]);
            seq.RemoveAt(0);
            Assert.AreEqual(KSEQOperationTypes.Remove, seq.lastOp?.op);

            seq = new KSEQReplicatedList<string>("test");
            seq.Insert(0, "a");
            seq.Insert(1, "b");
            seq.Insert(2, "c");
            Assert.AreEqual(3, seq.Count);
            seq.RemoveAt(1);
            Assert.AreEqual(seq.Count, 2);
            Assert.AreEqual(seq[1], "c");
        }

        [Test]
        public void TestApplyInsertNoExistingAtom()
        {
            var seq1 = new KSEQReplicatedList<string>("alice");
            var seq2 = new KSEQReplicatedList<string>("bob");
            seq1.Add("a");
            var op = seq1.lastOp;
            seq2.Apply(op);
            Assert.AreEqual(1, seq2.Count);
            Assert.AreEqual("a", seq2[0]);
        }

        [Test]
        public void TestApplyInsertAlreadyExists()
        {
            var seq = new KSEQReplicatedList<int>("test");
            seq.Add(42);
            var op1 = seq.lastOp;

            Assert.AreEqual(1, seq.Count);
            Assert.AreEqual(42, seq[0]);
            var op2 = new KSEQOperation<int>()
            {
                id = op1?.id,
                op = KSEQOperationTypes.Insert,
                replicaId = "test",
                realTime = DateTime.UtcNow.Ticks,
                value = 99
            };
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
        }

        [Test]
        public void TestApplySameIdentTwice()
        {
            var seq = new KSEQReplicatedList<int>("test");
            seq.Add(42);
            var op1 = seq.lastOp;
            seq.Add(99);
            seq.RemoveAt(0);
            Assert.AreEqual(1, seq.Count);
            var op2 = new KSEQOperation<int>()
            {
                id = op1?.id,
                op = KSEQOperationTypes.Insert,
                replicaId = "test",
                realTime = DateTime.UtcNow.Ticks,
                value = 123
            };
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
        }

        [Test]
        public void TestRemoveNonexistentAtomSilentlySucceeds()
        {
            var seq1 = new KSEQReplicatedList<string>("alice");
            var seq2 = new KSEQReplicatedList<string>("bob");
            seq1.Add("a");
            seq2.Add("b");
            seq1.RemoveAt(0);
            var op = seq1.lastOp;
            Assert.IsNotNull(op);
            seq2.Apply(op);
            Assert.AreEqual(1, seq2.Count);
            Assert.AreEqual("b", seq2[0]);
        }

        [Test]
        public void TestRemoveAtomApply()
        {
            var seq1 = new KSEQReplicatedList<int>("alice");
            var seq2 = new KSEQReplicatedList<int>("bob");
            seq1.Add(42);
            var insertOp = seq1.lastOp;
            seq2.Apply(insertOp);
            Assert.AreEqual(seq1.Count, seq2.Count);
            Assert.AreEqual(seq1.Count, 1);
            Assert.AreEqual(seq1[0], seq2[0]);
            seq2.RemoveAt(0);
            var removeOp = seq2.lastOp;
            seq1.Apply(removeOp);
            Assert.AreEqual(seq1.Count, seq2.Count);
            Assert.AreEqual(seq1.Count, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var o = seq1[0];
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var o = seq2[0];
            });
        }

        [Test]
        public void TestRemoveIdentTwice()
        {
            var seq = new KSEQReplicatedList<int>("test");
            seq.Add(42);
            var op1 = seq.lastOp;
            seq.Add(99);
            seq.RemoveAt(0);
            Assert.AreEqual(1, seq.Count);
            var op2 = new KSEQOperation<int>()
            {
                replicaId = "test",
                id = op1?.id,
                realTime = DateTime.UtcNow.Ticks,
                op = KSEQOperationTypes.Remove
            };
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
        }

        [Test]
        public void TestCRDTInsertRemove()
        {
            var seq = new KSEQReplicatedList<string>("alice");
            seq.Add("test");
            Assert.AreEqual(1, seq.Count);
            var ident = new Ident(1, new[] {new Segment(0, "bob"),});
            var op1 = new KSEQOperation<string>()
            {
                id = ident,
                replicaId = "bob",
                value = "hello",
                realTime = DateTime.UtcNow.Ticks,
                op = KSEQOperationTypes.Insert
            };
            var op2 = new KSEQOperation<string>()
            {
                id = ident,
                replicaId = "bob",
                realTime = DateTime.UtcNow.Ticks,
                op = KSEQOperationTypes.Remove
            };
            seq.Apply(op1);
            Assert.AreEqual(2, seq.Count);
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
        }

        [Test]
        public void TestInsertInsertDuplication()
        {
            var seq = new KSEQReplicatedList<string>("alice");
            seq.Add("test");
            Assert.AreEqual(1, seq.Count);
            var ident = new Ident(1, new[] {new Segment(0, "bob")});
            var op1 = new KSEQOperation<string>()
            {
                id = ident,
                replicaId = "bob", op = KSEQOperationTypes.Insert, realTime = DateTime.UtcNow.Ticks, value = "hello"
            };
            seq.Apply(op1);
            Assert.AreEqual(2, seq.Count);
            seq.Apply(op1);
            Assert.AreEqual(2, seq.Count);
        }

        [Test]
        public void TestRemoveRemoveDuplication()
        {
            var seq = new KSEQReplicatedList<string>("alice");
            seq.Add("test");
            Assert.AreEqual(1, seq.Count);
            var ident = new Ident(1, new[] {new Segment(0, "bob")});
            var op1 = new KSEQOperation<string>()
            {
                id = ident,
                op = KSEQOperationTypes.Insert,
                realTime = DateTime.UtcNow.Ticks,
                replicaId = "bob",
                value = "hello"
            };
            var op2 = new KSEQOperation<string>()
            {
                id = ident,
                op = KSEQOperationTypes.Remove,
                realTime = DateTime.UtcNow.Ticks,
                replicaId = "bob"
            };
            seq.Apply(op1);
            Assert.AreEqual(2, seq.Count);
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
        }

        [Test]
        public void TestCommutative()
        {
            var seq = new KSEQReplicatedList<string>("alice");
            seq.Add("test");
            Assert.AreEqual(1, seq.Count);
            var ident = new Ident(1, new[] {new Segment(0, "bob")});
            var op1 = new KSEQOperation<string>()
            {
                id = ident,
                op = KSEQOperationTypes.Insert,
                realTime = DateTime.UtcNow.Ticks,
                replicaId = "bob",
                value = "hello"
            };
            var op2 = new KSEQOperation<string>()
            {
                id = ident,
                op = KSEQOperationTypes.Remove,
                realTime = DateTime.UtcNow.Ticks,
                replicaId = "bob"
            };
            seq.Apply(op2);
            Assert.AreEqual(1, seq.Count);
            seq.Apply(op1);
            Assert.AreEqual(1, seq.Count);
        }

        [Test]
        public void TestRandomSingleReplica()
        {
            var random = new Random();
            var alice = new KSEQReplicatedList<int>("alice");
            var bob = new KSEQReplicatedList<int>("bob");
            var ops = Enumerable.Range(0, 1000).Select(i =>
            {
                alice.Insert(random.Next(i), i);
                return alice.lastOp;
            }).Concat(Enumerable.Range(0, 500)
                .Select(i =>
                {
                    alice.RemoveAt(random.Next(1000 - i));
                    return alice.lastOp;
                })).ToArray();

            Shuffle(ops, random);

            foreach (var op in ops)
            {
                if (op != null)
                {
                    bob.Apply(op.Value);
                }
            }

            Assert.AreEqual(alice.Count, bob.Count);
            Assert.AreEqual(500, alice.Count);
            Assert.IsTrue(alice.ToArray().SequenceEqual(bob.ToArray()));
        }

        [Test]
        public void TestRandomMultiReplica()
        {
            var random = new Random();
            var alice = new KSEQReplicatedList<int>("alice");
            var bob = new KSEQReplicatedList<int>("bob");

            KSEQOperation<int>?[] GenerateOps(KSEQReplicatedList<int> replica)
            {
                return Enumerable.Range(0, 1000).Select(i =>
                {
                    replica.Insert(random.Next(i), i);
                    return replica.lastOp;
                }).Concat(Enumerable.Range(0, 500)
                    .Select(i =>
                    {
                        replica.RemoveAt(random.Next(1000 - i));
                        return replica.lastOp;
                    })).ToArray();
            }

            var aliceOps = GenerateOps(alice);
            var bobOps = GenerateOps(bob);

            Shuffle(aliceOps, random);
            Shuffle(bobOps, random);

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

        public static void Shuffle<T>(IList<T> list, Random random)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}