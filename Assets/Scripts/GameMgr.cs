using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameMgr : MonoBehaviour
{
	class Placement	{ public int row, column; };

	[SerializeField] private TileBase greenTilePlayer, blueTileAI, pinkTileAvailable;
	[SerializeField] private List<Vector3Int> availableTiles;
	[SerializeField] private bool playerStarts = true;
	
	private Tilemap tileMap;
	private bool playerTurn;
	private bool gameEnd;

	private string playerIntString;
	private const int availableInt = 3;
	private const int playerInt = 1;
	private const int aiInt = 2;

	private const int boardSize = 3;
	private int[,] evaluationGrid;

	private void Start()
	{
		ResetGame();
	}

	private void Update()
	{
		if (!gameEnd)
		{
			GameOn();
		}
		else
		{
			if (Input.GetMouseButtonDown(0))
			{
				ResetGame();
			}
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			PrintEvaluationGridStatus();
		}
	}

	private void ResetGame()
	{
		tileMap = FindObjectOfType<Tilemap>();
		GetCells(tileMap);
		playerTurn = playerStarts ? true : false;
		gameEnd = false;

		evaluationGrid = new int[boardSize, boardSize]
		{
			{ availableInt, availableInt, availableInt },
			{ availableInt, availableInt, availableInt },
			{ availableInt, availableInt, availableInt },
		};
	}

	private void PrintEvaluationGridStatus()
	{
		for (int y= 0; y < evaluationGrid.GetLength(1); y++)
		{
			for (int x = 0; x < evaluationGrid.GetLength(0); x++)
			{
				switch (evaluationGrid[x,y])
				{
					case playerInt:
						playerIntString = "Green";
						break;
					case aiInt:
						playerIntString = "Blue";
						break;
					case availableInt:
						playerIntString = "Available";
						break;
				}
				Debug.Log($"Grid status: [ROW: {x}, COL: {y}] = {playerIntString}");
			}
		}
	}

	List<Vector3Int> GetCells(Tilemap tilemap)
	{
		availableTiles = new List<Vector3Int>();

		foreach (Vector3Int tileVector3Int in tilemap.cellBounds.allPositionsWithin)
		{
			Vector3Int tilePos = new Vector3Int(tileVector3Int.x, tileVector3Int.y, tileVector3Int.z);

			if (tilemap.HasTile(tilePos))
			{
				availableTiles.Add(tilePos);
				tileMap.SetTile(tilePos, pinkTileAvailable);
			}
		}
		return availableTiles;
	}

	private void GameOn()
	{
		if (playerTurn)
		{
			PlayerMove();
		}
		else
		{
			AIMove();
		}

		if (Evaluate(evaluationGrid) != 0 || availableTiles.Count == 0)
		{
			Debug.Log("- CLICK TO RESTART -");
			gameEnd = true;
		}
	}

	private void MakeMove(Vector3Int pos)
	{
		int player;
		string who;
		TileBase tile;

		if (playerTurn)
		{
			player = 1;
			tile = greenTilePlayer;
			who = ("Player");
		}
		else
		{
			player = 2;
			tile = blueTileAI;
			who = ("AI");
		}

		if (availableTiles.Contains(pos))
		{
			evaluationGrid[-pos.x, -pos.y] = player;
			tileMap.SetTile(pos, tile);
			availableTiles.Remove(pos);
			Debug.Log($"{who} Move: [ROW: {pos.x}, COL: { pos.y}]");

			playerTurn = !playerTurn ? true : false;
		}
	}

	private void PlayerMove()
	{
		if (playerTurn)
		{
			if (Input.GetMouseButtonDown(0))
			{
				Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				Vector3Int gridPlacement = tileMap.WorldToCell(mousePosition);

				MakeMove(gridPlacement);
			}
		}
	}
	
	private void AIMove()
	{
		Placement bestMove = FindBestMove(evaluationGrid);
		Vector3Int middleTile = new Vector3Int(-1, -1, 0);
		Vector3Int gridPlacement = availableTiles.Contains(middleTile) ? middleTile : new Vector3Int(-bestMove.row, -bestMove.column, 0);

		MakeMove(gridPlacement);
	}

	private bool AvailableTilesLeft(int[,] evalGrid)
	{
		for (int x = 0; x < boardSize; x++)
		{
			for (int y = 0; y < boardSize; y++)
			{
				if (evalGrid[x, y] == availableInt)
				{
					return true;
				}
			}
		}
		return false;
	}

	private int MiniMax(int[,] evalGrid, int depth)
	{
		int score = Evaluate(evalGrid);
		int best = 11;

		if (score == 10 || score == -10)
		{
			return score;
		}

		if (AvailableTilesLeft(evalGrid) == false)
		{
			return 0;
		}

		for (int x = 0; x < boardSize; x++)
		{
			for (int y = 0; y < boardSize; y++)
			{
				if (evalGrid[x, y] == availableInt)
				{
					evalGrid[x, y] = aiInt;
					best = Math.Min(best, MiniMax(evalGrid, depth +1));
					evalGrid[x, y] = availableInt;
				}
			}
		}
		return best;
	}

	private Placement FindBestMove(int[,] evalGrid)
	{
		int bestValue = -1000;
		Placement bestMove = new Placement();
		bestMove.row = 0;
		bestMove.column = 0;

		for (int x = 0; x < boardSize; x++)
		{
			for (int y = 0; y < boardSize; y++)
			{
				if (evalGrid[x, y] == availableInt)
				{
					evalGrid[x, y] = playerInt;
					int moveValue = MiniMax(evalGrid, 0);
					evalGrid[x, y] = availableInt;
					
					if (moveValue > bestValue)
					{
						bestMove.row = x;
						bestMove.column = y;
						bestValue = moveValue;
					}
				}
			}
		}
		return bestMove;
	}
	private int Evaluate(int[,] evalGrid)
	{
		for (int row = 0; row < boardSize; row++)
		{
			if (evalGrid[row, 0] == evalGrid[row, 1] &&
				evalGrid[row, 1] == evalGrid[row, 2])
			{
				if (evalGrid[row, 0] == playerInt)
				{
					return +10;
				}
				else if (evalGrid[row, 0] == aiInt)
				{
					return -10;
				}
			}
		}

		for (int column = 0; column < boardSize; column++)
		{
			if (evalGrid[0, column] == evalGrid[1, column] &&
				evalGrid[1, column] == evalGrid[2, column])
			{
				if (evalGrid[0, column] == playerInt)
				{
					return +10;
				}
				else if (evalGrid[0, column] == aiInt)
				{
					return -10;
				}
			}
		}

		if (evalGrid[0, 0] == evalGrid[1, 1] && evalGrid[1, 1] == evalGrid[2, 2])
		{
			if (evalGrid[0, 0] == playerInt)
			{
				return +10;
			}
			else if (evalGrid[0, 0] == aiInt)
			{
				return -10;
			}
		}

		if (evalGrid[0, 2] == evalGrid[1, 1] && evalGrid[1, 1] == evalGrid[2, 0])
		{
			if (evalGrid[0, 2] == playerInt)
			{
				return +10;
			}
			else if (evalGrid[0, 2] == aiInt)
			{
				return -10;
			}
		}
		return 0;
	}
}