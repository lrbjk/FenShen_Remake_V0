using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    [Header("µØÃæ¼ì²â")]
    public Transform groundCheckPoint;
    public Vector2 size;
    public LayerMask groundLayer;
    [Header("Ç½Ãæ¼ì²â")]
    public Transform wallCheckPoint;
    public Vector2 wallSize;
    public LayerMask wallLayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public bool CheckGround()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(groundCheckPoint.position, size, 0f, groundLayer);
        if(colliders.Length > 0)
        {
            return true;
        }
        return false;
    }
    public bool CheckWall()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(wallCheckPoint.position, wallSize, 0f, wallLayer);
        if (colliders.Length > 0)
        {
            return true;
        }
        return false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPoint.position,size);
        Gizmos.DrawWireCube(wallCheckPoint.position, wallSize);
    }
}
