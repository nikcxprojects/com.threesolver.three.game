using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> _types;
    [SerializeField] private int _winCondition = 243;

    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _looseScreen;
    [SerializeField] private Text _scoreText;

    [SerializeField] private AudioClip _mergeClip;

    private void OnEnable() => SwipeManager.SwipeEvent += SwipeEvent;
    private void OnDisable() => SwipeManager.SwipeEvent -= SwipeEvent;

    private List<Node> _nodes;
    private List<Block> _blocks;
    private GameState _state;
    private int _round;
    private int _score;

    private BlockType GetBlockTypeByValue(int value) => _types.First(t=>t.value == value);

    private void Start()
    {
        ChangeState(GameState.GenerateLevel);
    }

    private void ChangeState(GameState newState)
    {
        _state = newState;
        switch (newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlock:
                SpawnBlocks(_round++ == 0 ? 2 : 1);
                break;
            case GameState.WaitingInput:
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
                _winScreen.SetActive(true);
                Database.instance.AddNewScoreDatabase(_score);
                break;
            case GameState.Lose:
                _looseScreen.SetActive(true);
                Database.instance.AddNewScoreDatabase(_score);
                break;

        }
    }

    private void SwipeEvent(Vector2 direction)
    {
        if (_state != GameState.WaitingInput) return;
        Shift(direction);
    }

    private void GenerateGrid()
    {
        _round = 0;
        _score = 0;
        _nodes = new List<Node>();
        _blocks = new List<Block>();
        for(int x = 0; x < _width; x++)
        {
            for(int y = 0; y < _height; y++)
            {
                var node = Instantiate(_nodePrefab, new Vector3(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);
        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10.0f);
        ChangeState(GameState.SpawningBlock);
    }

    private void SpawnBlocks(int amount)
    {
        var freeNodes = _nodes.Where(n => n.occupiedBlock == null).OrderBy(b => Random.value).ToList();

        foreach (var node in freeNodes.Take(amount))
            SpawnBlock(node, 3);

        if(freeNodes.Count() == 1)
        {
            ChangeState(GameState.Lose);
            return;
        }

        ChangeState(_blocks.Any(b=>b.value == _winCondition) ? GameState.Win : GameState.WaitingInput);
    }

    private void SpawnBlock(Node node, int value) 
    {
        var block = Instantiate(_blockPrefab, node.pos, Quaternion.identity);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    private void Shift(Vector2 direction)
    {
        ChangeState(GameState.Moving);
        var orderedBlocks = _blocks.OrderBy(b => b.pos.x).ThenBy(b => b.pos.y);
        if (direction == Vector2.right || direction == Vector2.up)
            orderedBlocks.Reverse();
        
        foreach(var block in orderedBlocks)
        {
            var next = block.node;
            do
            {
                block.SetBlock(next);
                var possibleNode = GetNodeAtPosition(next.pos + direction);
                if(possibleNode != null)
                {
                    if(possibleNode.occupiedBlock != null && possibleNode.occupiedBlock.CanMerge(block.value))
                        block.MergeBlock(possibleNode.occupiedBlock);
                    else if (possibleNode.occupiedBlock == null)
                        next = possibleNode;
                }

            } while (next != block.node);            
        }

        var sequence = DOTween.Sequence();
        foreach(var block in orderedBlocks)
        {
            var movePoint = block.merginBlock != null ? block.merginBlock.node.pos : block.node.pos;
            sequence.Insert(0, block.transform.DOMove(movePoint, 0.2f));
        }

        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b => b.merginBlock != null))
            {
                MergeBlocks(block.merginBlock, block);
            }

            ChangeState(GameState.SpawningBlock);
        });
    }

    private void MergeBlocks(Block baseBlock, Block merginBlock)
    {
        _score += 100;
        _scoreText.text = $"Score: {_score}";
        AudioManager.instance.PlayAudio(_mergeClip);

        SpawnBlock(baseBlock.node, baseBlock.value * 3);
        RemoveBlock(merginBlock);
        RemoveBlock(baseBlock);
    }

    private void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    private Node GetNodeAtPosition(Vector2 pos)
    {
        return _nodes.FirstOrDefault(n => n.pos == pos);
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    public void Exit()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}

[System.Serializable]
public struct BlockType
{
    public int value;
    public Color color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlock,
    WaitingInput,
    Moving,
    Win,
    Lose
}
