using UnityEngine;
using TMPro;
public class DeleteManage : MonoBehaviour
{
    public TMP_InputField TextInput;
    public void deleteText()
    {
        TextInput.text = "";
    }

}
