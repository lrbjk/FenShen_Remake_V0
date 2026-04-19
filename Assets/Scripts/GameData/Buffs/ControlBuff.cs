namespace FenShen.GameData
{
    public class ControlBuff : BuffBase
    {
        public override void OnApply()
        {
            UpdateFlags(1);
        }

        public override void OnRemove()
        {
            UpdateFlags(-1);
        }

        private void UpdateFlags(int delta)
        {
            if (instance == null || instance.controller == null || instance.data == null)
            {
                return;
            }

            instance.controller.AdjustControlFlags(instance.data.controlFlags, delta);
        }
    }
}
