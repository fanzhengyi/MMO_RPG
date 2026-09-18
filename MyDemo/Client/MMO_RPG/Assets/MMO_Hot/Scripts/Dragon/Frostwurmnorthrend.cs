using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：主界面冰霜巨龙特效控制
//***************************************** 
public class Frostwurmnorthrend : MonoBehaviour
{
    private ParticleSystem smokePS;
    private ParticleSystem firePS;

    private void Awake()
    {
        smokePS = transform.Find("Smoke").GetComponent<ParticleSystem>();
        firePS = DeepFindTransform.DeepFindChild(transform, "FrostSpray")
            .GetComponent<ParticleSystem>();
        HideAll();
    }


    public void PlayGroundSmoke()
    {
        smokePS.gameObject.SetActive(true);
        smokePS.Play();
    }

    public void PlayIceFire()
    {
        firePS.gameObject.SetActive(true);
        firePS.Play();
    }

    public void StopIceFire()
    {
        firePS.Stop();
    }

    public void HideAll()
    {
        firePS.Stop();
        smokePS.Stop();
        firePS.gameObject.SetActive(false);
        smokePS.gameObject.SetActive(false);
    }
}
