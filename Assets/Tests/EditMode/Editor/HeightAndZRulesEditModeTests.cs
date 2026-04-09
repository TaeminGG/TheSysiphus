using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HeightAndZRulesEditModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForEditModeHeightTests()
    {
        Assert.Pass("EditMode height/Z test scaffold is ready.");
    }

    [Test]
    public void HeightRule_ReturnsGroundZLevel_WhenSupportingTileExists()
    {
        var root = new GameObject("HeightRuleTestRoot");
        try
        {
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform);
            var grid = gridObject.AddComponent<Grid>();

            var tilemapObject = new GameObject("bmap");
            tilemapObject.transform.SetParent(gridObject.transform);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();

            var checkerObject = new GameObject("SurroundTileCheck");
            checkerObject.transform.SetParent(root.transform);
            var checker = checkerObject.AddComponent<SurroundTileCheck>();
            checker.grid = grid;
            checker.bmap = tilemap;

            var tile = ScriptableObject.CreateInstance<Tile>();
            tilemap.SetTile(new Vector3Int(-2, -2, 2), tile);

            var groundZ = checker.GetGroundZLevel(Vector3.zero);

            Assert.That(groundZ, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HeightRule_ReturnsMinusOne_WhenSupportingTileDoesNotExist()
    {
        var root = new GameObject("HeightRuleEmptyRoot");
        try
        {
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform);
            var grid = gridObject.AddComponent<Grid>();

            var tilemapObject = new GameObject("bmap");
            tilemapObject.transform.SetParent(gridObject.transform);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();

            var checkerObject = new GameObject("SurroundTileCheck");
            checkerObject.transform.SetParent(root.transform);
            var checker = checkerObject.AddComponent<SurroundTileCheck>();
            checker.grid = grid;
            checker.bmap = tilemap;

            var groundZ = checker.GetGroundZLevel(Vector3.zero);

            Assert.That(groundZ, Is.EqualTo(-1));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    [Ignore("TODO: Extract fall-start rule and verify falling transitions when no supporting tile exists.")]
    public void HeightRule_TransitionsToFall_WhenGroundIsMissing()
    {
    }

    [Test]
    [Ignore("TODO: Extract attack height comparison logic from AttackHitBox/AttackData and verify upper/lower range hits.")]
    public void AttackHeightRule_HitsTarget_WithinUpperAndLowerRange()
    {
    }

    [Test]
    [Ignore("TODO: Extract attack height comparison logic from AttackHitBox/AttackData and verify out-of-range misses.")]
    public void AttackHeightRule_IgnoresTarget_OutsideUpperAndLowerRange()
    {
    }
}
