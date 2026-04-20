using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public Vector2 size;
    public LayerMask groundLayer;

    [Header("Lose Ground Grace")]
    [Min(0)]
    public int loseGroundGraceFrames = 3;
    [Min(0f)]
    public float loseGroundGraceTime = 0.05f;

    [Header("Wall Check")]
    public Transform wallCheckPoint;
    public Vector2 wallSize;
    public LayerMask wallLayer;

    private bool _isGrounded;
    private int _consecutiveUngroundedFrames;
    private float _lastGroundedTime = float.NegativeInfinity;
    private int _lastGroundEvalFrame = -1;
    private bool _lastGroundEvalResult;

    void Start()
    {
        bool groundedNow = CheckGroundRaw();
        _isGrounded = groundedNow;
        _lastGroundEvalResult = groundedNow;

        if (groundedNow)
        {
            _lastGroundedTime = Time.time;
        }
    }

    void Update()
    {
    }

    public bool CheckGround()
    {
        if (_lastGroundEvalFrame == Time.frameCount)
        {
            return _lastGroundEvalResult;
        }

        _lastGroundEvalFrame = Time.frameCount;

        bool groundedNow = CheckGroundRaw();
        if (groundedNow)
        {
            _isGrounded = true;
            _consecutiveUngroundedFrames = 0;
            _lastGroundedTime = Time.time;
            _lastGroundEvalResult = true;
            return true;
        }

        _consecutiveUngroundedFrames++;

        bool exceededFrameGrace = loseGroundGraceFrames <= 0 || _consecutiveUngroundedFrames > loseGroundGraceFrames;
        bool exceededTimeGrace = loseGroundGraceTime <= 0f || Time.time - _lastGroundedTime > loseGroundGraceTime;

        if (exceededFrameGrace && exceededTimeGrace)
        {
            _isGrounded = false;
        }

        _lastGroundEvalResult = _isGrounded;
        return _isGrounded;
    }

    public bool CheckWall()
    {
        if (wallCheckPoint == null)
        {
            return false;
        }

        Collider2D[] colliders = Physics2D.OverlapBoxAll(wallCheckPoint.position, wallSize, 0f, wallLayer);
        return colliders.Length > 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (groundCheckPoint != null)
        {
            Gizmos.DrawWireCube(groundCheckPoint.position, size);
        }

        if (wallCheckPoint != null)
        {
            Gizmos.DrawWireCube(wallCheckPoint.position, wallSize);
        }
    }

    private bool CheckGroundRaw()
    {
        if (groundCheckPoint == null)
        {
            return false;
        }

        Collider2D[] colliders = Physics2D.OverlapBoxAll(groundCheckPoint.position, size, 0f, groundLayer);
        return colliders.Length > 0;
    }
}
