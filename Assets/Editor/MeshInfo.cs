 using UnityEngine;
 using System.Collections;
 using UnityEditor;
 
 public class MeshInfo : EditorWindow
 {
     private int vertexCount;
     private int submeshCount;
     private int triangleCount;
 
     [MenuItem("Tools/Mesh Info")]
     static void Init()
     {
         // Get existing open window or if none, make a new one:
         MeshInfo window = (MeshInfo)EditorWindow.GetWindow(typeof(MeshInfo));
         window.titleContent.text = "Mesh Info";
     }
 
     void OnSelectionChange()
     {
         Repaint();
     }
 
     void OnGUI()
     {
         vertexCount = 0;
         triangleCount = 0;
         submeshCount = 0;
 
         foreach (GameObject g in Selection.gameObjects)
         {
             foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>())
             {
                 vertexCount += mf.sharedMesh.vertexCount;
                 triangleCount += mf.sharedMesh.triangles.Length / 3;
                 submeshCount += mf.sharedMesh.subMeshCount;
             }
 
             foreach (SkinnedMeshRenderer smr in g.GetComponentsInChildren<SkinnedMeshRenderer>())
             {
                 vertexCount += smr.sharedMesh.vertexCount;
                 triangleCount += smr.sharedMesh.triangles.Length / 3;
                 submeshCount += smr.sharedMesh.subMeshCount;
             }
         }
 
         EditorGUILayout.LabelField("Vertices: ", vertexCount.ToString());
         EditorGUILayout.LabelField("Triangles: ", triangleCount.ToString());
         EditorGUILayout.LabelField("SubMeshes: ", submeshCount.ToString());
     }
 }