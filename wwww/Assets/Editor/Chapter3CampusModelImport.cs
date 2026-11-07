#if UNITY_EDITOR
using UnityEditor;
// These authored background meshes feed the shared, lightweight crowd rig.
// Read/write is required once for skin weights; no importer changes elsewhere.
public sealed class Chapter3CampusModelImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if(!assetPath.StartsWith("Assets/Resources/Chapter3/CampusCharacters/")||!assetPath.EndsWith(".fbx"))return;
        var importer=(ModelImporter)assetImporter;
        importer.isReadable=true;importer.importAnimation=false;importer.addCollider=false;
        importer.animationType=ModelImporterAnimationType.None;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.importNormals=ModelImporterNormals.Import;
    }
}
#endif
