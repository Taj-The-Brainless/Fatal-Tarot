using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class timerChal : MonoBehaviour
{

    public TMP_Text timerText;
    public Image timerFillImage;
    public Image timerRangeImage;
    public float timerDuration = 1f;
    public float timerRange = 0.5f; // +/- range for successful hit
    public bool success = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {


        timerText.text = "Timer Challenge!";
        timerFillImage.fillAmount = 1f;
        timerRangeImage.fillAmount = timerRange / timerDuration;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public bool OnEnable(){
        timerRangeImage.fillAmount = timerRange / timerDuration;
        StartCoroutine(TimerChallenge());
        return success;
    }

    private IEnumerator TimerChallenge(){
        float timer = 0f;
        success = false;
        
        //decrease timerFill image fill amount over time
        while(timer < timerDuration){
            timer += Time.deltaTime;
            timerFillImage.fillAmount = 1f - (timer / timerDuration);
            if(Input.GetKeyDown(KeyCode.Space)){
                if(Mathf.Abs(timer - timerDuration) <= timerRange){
                    success = true;
                    break;
                } else {
                    break;
                }
            }
            yield return null;
        }
        if(success){
            timerText.text = "Success!";
        } else {
            timerText.text = "Fail";
        }

        //wait 2 seconds
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }
    
}
