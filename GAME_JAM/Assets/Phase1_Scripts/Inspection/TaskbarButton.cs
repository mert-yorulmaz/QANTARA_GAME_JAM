using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Görev çubuğundaki pencere düğmesi.
public class TaskbarButton : MonoBehaviour
{
    public Button button;
    public Image background;
    public Image icon;
    public TMP_Text label;
    public Sprite normal;
    public Sprite active;

    public void SetActiveLook(bool isActive)
    {
        background.sprite = isActive ? active : normal;
    }
}
