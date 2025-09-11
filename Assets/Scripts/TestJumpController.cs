using UnityEngine;

namespace MOBA
{
    public class TestJumpController : MonoBehaviour
    {
        private JumpController jumpController;
        
        void Start()
        {
            jumpController = new JumpController();
            Debug.Log("JumpController test successful");
        }
    }
}
