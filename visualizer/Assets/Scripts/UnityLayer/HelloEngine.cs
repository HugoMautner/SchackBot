using UnityEngine;
using SchackBot.Engine;

public class HelloEngine : MonoBehaviour
{
    void Start()
    {
        // Touch ANY type from the engine so Unity must resolve the DLL.
        Debug.Log("Engine linked. Type check: " + typeof(Class1).FullName);
    }
}
