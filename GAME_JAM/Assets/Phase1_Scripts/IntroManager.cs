using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    public Image fadePanel;

    public float fadeDuration = 2f;

    void Start()
    {
        Debug.Log("Intro baþladý");
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        Color color = fadePanel.color;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            fadePanel.color = color;

            yield return null;
        }

        color.a = 0f;
        fadePanel.color = color;

        fadePanel.gameObject.SetActive(false);
    }
}