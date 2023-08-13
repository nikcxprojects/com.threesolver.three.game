using UnityEngine;
using TMPro;

public class Block : MonoBehaviour
{
    public int value;
    public Node node;
    public Block merginBlock;
    public bool isMerging;
    public Vector2 pos => transform.position;

    [SerializeField] private SpriteRenderer _render;
    [SerializeField] private TextMeshPro _text;
    public void Init(BlockType type)
    {
        value = type.value;
        _render.color = type.color;
        _text.text = type.value.ToString();
    }

    public void SetBlock(Node node)
    {
        if (this.node != null) 
            this.node.occupiedBlock = null;

        this.node = node;
        this.node.occupiedBlock = this;
    }

    public void MergeBlock(Block blockToMergeWith)
    {
        merginBlock = blockToMergeWith;
        node.occupiedBlock = null;
        blockToMergeWith.isMerging = true;
    }

    public bool CanMerge(int value) => value == this.value && !isMerging && merginBlock == null;
}
