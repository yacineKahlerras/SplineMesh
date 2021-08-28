using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Projectile))]
public class ProjectileEditor : Editor
{
    private void OnSceneGUI()
    {
        Projectile projectile = target as Projectile;
        Transform tranform = projectile.transform;
        projectile.damageRadius = Handles.RadiusHandle(tranform.rotation, tranform.position, projectile.damageRadius);
    }
}
