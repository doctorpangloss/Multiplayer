using System;
using System.Collections.Generic;

namespace HiddenSwitch.Networking.Peers.Internal
{
    internal sealed class LSEQIdentGenerator
    {
        private Random m_Random = new Random();

        public enum LSEQStrategy
        {
            AddFromLeft = 1,
            SubtractFromRight = 2
        }

        public int startingWidth { get; }
        public int maxDistance { get; }
        public IList<LSEQStrategy> strategies { get; }
        public Ident first { get; private set; }
        public Ident last { get; private set; }

        public LSEQIdentGenerator(int startingWidth = 4, int maxDistance = 10)
        {
            this.startingWidth = startingWidth;
            this.maxDistance = maxDistance;
            strategies = new List<LSEQStrategy>(startingWidth + maxDistance + 1);
        }

        public Ident GetIdent(string name, long time, Ident before, Ident after)
        {
            if (before == null)
            {
                before = GetFirst(name);
            }

            if (after == null)
            {
                after = GetLast(name);
            }

            var distance = 0;
            var depth = -1;
            var min = 0;
            var max = 0;
            while (distance < 1)
            {
                depth++;
                min = depth < before.depth ? before.Get(depth).digit : 0;
                max = depth < after.depth ? after.Get(depth).digit : GetWidthAtDepth(depth);
                distance = max - min - 1;
            }

            var boundary = Math.Min(distance, maxDistance);
            var delta = (int) Math.Floor(GetRandom() * boundary) + 1;
            var strategy = GetStrategyAtDepth(depth);
            var path = new List<Segment>(depth + 1);
            for (var i = 0; i < depth; i++)
            {
                path.Add(i < before.depth ? before.Get(i) : new Segment(0, name));
            }

            switch (strategy)
            {
                case LSEQStrategy.AddFromLeft:
                    path.Add(new Segment(min + delta, name));
                    break;
                case LSEQStrategy.SubtractFromRight:
                    path.Add(new Segment(max - delta, name));
                    break;
            }

            return new Ident(time, path.ToArray());
        }

        private LSEQStrategy GetStrategyAtDepth(int depth)
        {
            while (depth >= strategies.Count)
            {
                strategies.Add(GetRandom() > .5f ? LSEQStrategy.AddFromLeft : LSEQStrategy.SubtractFromRight);
            }

            return strategies[depth];
        }


        private float GetRandom()
        {
            return (float) m_Random.NextDouble();
        }


        private int GetWidthAtDepth(int depth)
        {
            return (1 << Math.Min(depth + startingWidth, 32)) - 1;
        }

        private Ident GetFirst(string name)
        {
            if (first == null)
            {
                first = new Ident(0, new[] {new Segment(0, name)});
            }

            return first;
        }


        private Ident GetLast(string name)
        {
            if (last == null)
            {
                last = new Ident(0, new[] {new Segment(GetWidthAtDepth(0), name)});
            }

            return last;
        }
    }
}