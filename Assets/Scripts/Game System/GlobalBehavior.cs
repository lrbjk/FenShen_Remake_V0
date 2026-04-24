using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 全局的游戏管理器，负责处理跨场景的/后绑定的/弱引用的事件。它的执行顺序必须在其他所有自定义代码之前
/// </summary>
public class GlobalBehavior : MonoBehaviour
{
    private static GlobalBehavior _instance;
    
    public static GlobalBehavior Instance { get{
            if(_instance == null){
                _instance = new();
            }
            return _instance;
        }
        set{
            _instance = value;
        }
    }
    
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            // await GlobalLoad(); 加载存档
            
            Initialize().Forget();
            DontDestroyOnLoad(gameObject);
            // inputControlScheme = new();
            // LogActionMapStatus();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化游戏
    /// </summary>
    private async UniTaskVoid Initialize()
    {
        await UniTask.Yield();
        await UniTask.Delay(3000);
        await ShowCommonUIBase();
    }
    
    // ========================
    [SerializeField] private CommonUIBase _commonUI;
    public async UniTask ShowCommonUIBase()
    {
        _commonUI.SetVisibility((true));
        await UniTask.WaitUntil(() => _commonUI.PopupResult != PopupResult.Unset);
        _commonUI.SetVisibility(false);
    }
}
