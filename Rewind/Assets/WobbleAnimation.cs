using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class WobbleAnimation : MonoBehaviour
{
    [SerializeField] private AnimInfo[] animInfos;

    protected virtual void Update() {
        CustomAnimation(animInfos);
    }

    protected void CustomAnimation(AnimInfo[] animInfos) {
        foreach (AnimInfo anim in animInfos) {
            if (anim.transform != null) {
                anim.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(Time.time * anim.RotationSpeed) * anim.RotationRange);
            }
        }
    }
}

[Serializable]
public struct AnimInfo {
    public Transform transform;
    public float RotationSpeed, RotationRange;

    [Space]

    public SpriteRenderer spriteRenderer;
    public Color color;
    public Sprite sprite;
}


