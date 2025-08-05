using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;


// 공통사용되는 함수들
public abstract class RegisterInputControl : MonoBehaviour, InputControl
{
    protected GameObject Cam;
    protected virtual void Start()
    {
        RegisterControl();
        Cam = Camera.main.gameObject;
    }

    // Start시 씬에서 InputManager의 Control을 자신으로 변경
    private void RegisterControl()
    {
        InputManager.Instance.SetInputControl(this); 
    }

    public virtual void SetCurrentInput(int page, int index)
    {
    }

    public virtual void ExecuteInput(Key key, bool performed)
    {
    }

    public virtual void ChangeIndex()
    {
    }
}
