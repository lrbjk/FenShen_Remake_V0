using System.Collections.Generic;

namespace FenShen.GameData
{
    public class CompositeBuff : BuffBase
    {
        private readonly List<IBuff> children = new List<IBuff>();

        public CompositeBuff(params IBuff[] parts)
        {
            if (parts == null)
            {
                return;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null)
                {
                    children.Add(parts[i]);
                }
            }
        }

        public override void Initialize(BuffInstance buffInstance)
        {
            base.Initialize(buffInstance);

            for (int i = 0; i < children.Count; i++)
            {
                children[i].Initialize(buffInstance);
            }
        }

        public override void OnApply()
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].OnApply();
            }
        }

        public override void OnRemove()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                children[i].OnRemove();
            }
        }

        public override void OnRefresh()
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].OnRefresh();
            }
        }

        public override void OnTick()
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].OnTick();
            }
        }

        public override void OnStackChanged(int previousStack, int newStack)
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].OnStackChanged(previousStack, newStack);
            }
        }
    }
}
