using Naninovel;
using UnityEngine;
using UnityEngine.UIElements;


public class NECCamera : NaninovelEasyCopy
{
    public Transform TargetObj { get; set; }

    private Camera _camera;

    // make prop if null then get children
    public Camera Camera
    {
        get
        {
            if (_camera == null)
            {
                _camera = TargetObj.GetComponentInChildren<Camera>();
            }
            return _camera;
        }
    }

    private void LateUpdate()
    {
        if (TargetObj == null)
        {
            Destroy(gameObject);
            return;
        }
    }
}

public class NECCharacter : NaninovelEasyCopy
{
    public Transform TargetObj { get; set; }
    private TransitionalRenderer _renderer;
    public TransitionalRenderer Renderer
    {
        get
        {
            if (_renderer == null)
                _renderer = TargetObj.GetComponentInChildren<TransitionalRenderer>();
            return _renderer;
        }
    }

    private ICharacterActor _actor;

    public ICharacterActor Actor
    {
        get
        {
            _actor ??= Engine.GetService<ICharacterManager>().GetActor(ActorName);
            return _actor;
        }
    }

    public string ActorName => TargetObj.gameObject.name;


    private void LateUpdate()
    {
        if (TargetObj == null)
        {
            Destroy(gameObject);
            return;
        }

    }
}

public class NaninovelEasyCopy : MonoBehaviour
{
}