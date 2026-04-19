namespace FenShen.GameData
{
    public interface IBuff
    {
        BuffInstance Instance { get; }
        bool IsExpired { get; }

        void Initialize(BuffInstance instance);
        void OnApply();
        void OnRemove();
        void OnRefresh();
        void OnTick();
        void OnStackChanged(int previousStack, int newStack);
        void Update(float deltaTime);
    }
}
