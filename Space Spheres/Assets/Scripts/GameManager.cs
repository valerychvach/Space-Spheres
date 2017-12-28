﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    // Serializable fields
    // Ground
    [SerializeField] private Transform groundPrefab;
    [SerializeField] private Transform groundContainer;
    // Sphere
    [SerializeField] private Transform spherePrefab;
    [SerializeField] private Transform sphereContainer;
    [SerializeField] private Material[] sphereMaterials;
    // Color
    [SerializeField] private Transform colorContainer;
    [SerializeField] private Material[] colorMaterials;

    // Actions
    public event Action<Transform, MoveSide, bool> SphereGo;
    public event Action<MoveSide, bool> PlayerGo;
    public event Action<bool> ColorGo;

    // Path grid
    [HideInInspector] public Vector2[,] pathGrid;
    [HideInInspector] public Vector2 density;
    [HideInInspector] public List<Vector2> spherePosOnGrid;
    [HideInInspector] public Vector2 playerPosOnGrid;
    [HideInInspector] public Vector2 stepBetweenGround;

    // Colors, ground
    private List<Transform> colorList;
    private List<Vector2> colorPosOnGrid;
    private List<Colors> colorColor;
    private List<bool> colorReadyToDestroy;
    private float localSizeGroundCoef = 1.1f;
    private float heightGroundPrefab = 0.1f;
    // Player
    private Transform player;
    private MoveSide playerMoveSide;
    private bool playerSwap;
    private Colors playerColor;
    // Spheres
    private List<Transform> sphereList;
    private List<MoveSide> sphereMoveSide;
    private List<bool> sphereSwap;
    private List<Colors> sphereColor;
    
    

    // enums
    private enum ObjForRandom
    {
        Player,
        Sphere,
        Color
    }

    public enum MoveSide
    {
        Up,
        Down,
        Left,
        Right
    }

    private enum Colors
    {
        Blue,
        Red,
        Green,
        Orange
    }

    private void Start()
    {
        // Actions
        InputHandler.Instance.SwipeUp += SwipeUp;
        InputHandler.Instance.SwipeDown += SwipeDown;
        InputHandler.Instance.SwipeLeft += SwipeLeft;
        InputHandler.Instance.SwipeRight += SwipeRight;
        InputHandler.Instance.Tap += Tap;
        // Create level
        CreateLevel(
            _density: new Vector2(7, 13), // Ground resolution (non-paired)
            nSphere: 2,                  // Number of sphere (max 4)
            nColors: 2);                 // Number of color (max 4)

        StartCoroutine(PlayOneStep());
    }

    private IEnumerator PlayOneStep()
    {
        int nStep = 50;
        float timeToMove = 0.7f;
        
        // Calculate moving for all spheres and player
        for (int j = 0; j < nStep; j++)
        {
            for (int i = 0; i < sphereList.Count; i++)
            {
                CalculateMovingSpheres(sphereList[i]);
            }
            CalculateMovingPlayer();

            yield return new WaitForSeconds(timeToMove);

            PlayerGo.Invoke(playerMoveSide, playerSwap);
        }


        

    }

    private void CreateLevel(Vector2 _density, int nSphere, int nColors)
    {
        density = new Vector2(_density.x, _density.y);

        CreateGround();
        CreateSpheres(nSphere);
        CreateColors(nColors);
    }

    private void CreateGround()
    {
        float[] xRange = new float[2] { -5.2f, 5.2f };
        float[] yRange = new float[2] { -9.5f, 9.5f };

        stepBetweenGround = new Vector2
        {
            x = (xRange[1] * 2) / ((float)density.x),
            y = (yRange[1] * 2) / ((float)density.y)
        };

        Vector2 startMatrixPosition = new Vector2
        {
            x = (2 - (density.x - 1) * stepBetweenGround.x) / 2 - 1,
            y = (2 - (density.y - 1) * stepBetweenGround.y) / 2 - 1
        };

        pathGrid = new Vector2[(int)density.y, (int)density.x];

        for (int i = 0; i < density.y; i++)
        {
            for (int j = 0; j < density.x; j++)
            {
                pathGrid[i, j] = new Vector2(startMatrixPosition.x + stepBetweenGround.x * j, startMatrixPosition.y + stepBetweenGround.y * i);
                Transform groundGO = Instantiate(groundPrefab, pathGrid[i, j], Quaternion.identity, groundContainer);
                groundGO.position = new Vector3(pathGrid[i, j].x, 0, pathGrid[i, j].y);
                groundGO.localScale = new Vector3(stepBetweenGround.x / localSizeGroundCoef, heightGroundPrefab, stepBetweenGround.y / localSizeGroundCoef);
            }
        }
    }

    private void CreateSpheres(int nSphere)
    {
        sphereList = new List<Transform>();
        spherePosOnGrid = new List<Vector2>();
        sphereSwap = new List<bool>();
        sphereColor = new List<Colors>();
        sphereMoveSide = new List<MoveSide>();

        playerPosOnGrid = RandomPosition(density, ObjForRandom.Player);
        //playerPosOnGrid = new Vector2(2, 2);
        player = Instantiate(spherePrefab, sphereContainer);
        player.position = new Vector3(pathGrid[
        (int)(playerPosOnGrid.x), (int)(playerPosOnGrid.y)].x,
        player.localScale.y / 2,
        pathGrid[(int)(playerPosOnGrid.x), (int)(playerPosOnGrid.y)].y);
        MeshRenderer meshRendererPlayer = player.GetComponent<MeshRenderer>();
        meshRendererPlayer.material = sphereMaterials[0];
        playerMoveSide = MoveSide.Down;
        playerColor = Colors.Blue;
        player.gameObject.AddComponent<PlayerController>();

        for (int i = 0; i < nSphere - 1; i++)
        {
            Transform sphere = Instantiate(spherePrefab, sphereContainer);
            Vector2 randomPos = RandomPosition(density, ObjForRandom.Sphere);
            float sphereHeightAboveGround = sphere.localScale.y / 2 * -1;
            sphere.position = new Vector3(pathGrid[(int)(randomPos.x), (int)(randomPos.y)].x, sphereHeightAboveGround, pathGrid[(int)(randomPos.x), (int)(randomPos.y)].y);
            MeshRenderer meshRendererSphere = sphere.GetComponent<MeshRenderer>();
            meshRendererSphere.material = sphereMaterials[i+1];
            sphereList.Add(sphere);
            spherePosOnGrid.Add(randomPos);
            sphereMoveSide.Add(GetRandomMoveSide());
            sphereSwap.Add(false);
            if (i == 0)
                sphereColor.Add(Colors.Blue);
            if (i == 1)
                sphereColor.Add(Colors.Red);
            if (i == 2)
                sphereColor.Add(Colors.Green);
            if (i == 3)
                sphereColor.Add(Colors.Orange);
        }
    }

    private MoveSide GetRandomMoveSide()
    {
        MoveSide randomSide = MoveSide.Up;
        int random = UnityEngine.Random.Range(0, 4);

        switch (random)
        {
            case 0:
                randomSide = MoveSide.Up;
                break;
            case 1:
                randomSide = MoveSide.Right;
                break;
            case 2:
                randomSide = MoveSide.Down;
                break;
            case 3:
                randomSide = MoveSide.Left;
                break;
        }

        return randomSide;
    }

    private void CreateColors(int nColors)
    {
        colorList = new List<Transform>();
        colorPosOnGrid = new List<Vector2>();
        colorColor = new List<Colors>();
        colorReadyToDestroy = new List<bool>();

        for (int i = 0; i < nColors; i++)
        {
            Transform color = Instantiate(groundPrefab, sphereContainer);
            Vector2 randomPos = RandomPosition(density, ObjForRandom.Color);
            color.position = new Vector3(pathGrid[(int)(randomPos.x), (int)(randomPos.y)].x, 0f, pathGrid[(int)(randomPos.x), (int)(randomPos.y)].y);
            MeshRenderer meshRendererColor = color.GetComponent<MeshRenderer>();
            meshRendererColor.material = colorMaterials[i];
            color.gameObject.AddComponent<Collider>();
            color.localScale = new Vector3(stepBetweenGround.x / localSizeGroundCoef, heightGroundPrefab, stepBetweenGround.y / localSizeGroundCoef);
            colorList.Add(color);
            colorPosOnGrid.Add(randomPos);
            if (i == 0)
                colorColor.Add(Colors.Blue);
            if (i == 1)
                colorColor.Add(Colors.Red);
            if (i == 2)
                colorColor.Add(Colors.Green);
            if (i == 3)
                colorColor.Add(Colors.Orange);
            colorReadyToDestroy.Add(false);
        }
    }

    private Vector2 RandomPosition(Vector2 density, ObjForRandom obj)
    {
        bool readyCalculate = true;
        bool nextIteration = true;
        int x = 0, y = 0, rndX = 0, rndY = 0;
        Vector2 newPosition;

        while(readyCalculate)
        {
            nextIteration = true;
            
            if (obj == ObjForRandom.Player)
            {
                rndX = UnityEngine.Random.Range((int)0, ((int)density.x - 1) / 2) * 2;
                rndY = UnityEngine.Random.Range((int)0, ((int)density.y - 1) / 2) * 2;
            }
            else if (obj == ObjForRandom.Sphere)
            {
                rndX = UnityEngine.Random.Range((int)0, ((int)density.x - 1) / 2) * 2 + 1;
                rndY = UnityEngine.Random.Range((int)0, ((int)density.y - 1) / 2) * 2 + 1;
                newPosition = new Vector2(rndY, rndX);

                for (int i = 0; i < spherePosOnGrid.Count; i++)
                {
                    if (newPosition == spherePosOnGrid[i])
                    {
                        nextIteration = false;
                    }
                }
            }
            else if (obj == ObjForRandom.Color)
            {
                rndX = UnityEngine.Random.Range((int)0, ((int)density.x - 1) / 2) * 2;
                rndY = UnityEngine.Random.Range((int)0, ((int)density.y - 1) / 2) * 2;
                newPosition = new Vector2(rndY, rndX);

                for (int i = 0; i < colorPosOnGrid.Count; i++)
                {
                    if (newPosition == colorPosOnGrid[i] && newPosition == playerPosOnGrid)
                    {
                        nextIteration = false;
                    }
                }
            }

            if (nextIteration)
            {
                x = rndY;
                y = rndX;
                readyCalculate = false;
            }
        }
        
        return new Vector2(x, y);
    }

    private void CalculateMovingPlayer()
    {
        // Calculate for player and spheres
        for (int i = 0; i < spherePosOnGrid.Count; i++)
        {
            // Check to swap with player
            if (playerMoveSide == MoveSide.Right &&
                sphereMoveSide[i] == MoveSide.Up &&
                spherePosOnGrid[i].x - 1 == playerPosOnGrid.x &&
                spherePosOnGrid[i].y + 1 == playerPosOnGrid.y)
            {
                sphereSwap[i] = true;
                playerSwap = true;
            }
            else if (playerMoveSide == MoveSide.Right &&
                sphereMoveSide[i] == MoveSide.Down &&
                spherePosOnGrid[i].x - 1 == playerPosOnGrid.x &&
                spherePosOnGrid[i].y - 1 == playerPosOnGrid.y)
            {
                sphereSwap[i] = true;
                playerSwap = true;
            }
            else if (playerMoveSide == MoveSide.Left &&
                sphereMoveSide[i] == MoveSide.Down &&
                spherePosOnGrid[i].x + 1 == playerPosOnGrid.x &&
                spherePosOnGrid[i].y - 1 == playerPosOnGrid.y)
            {
                sphereSwap[i] = true;
                playerSwap = true;
            }
            else if (playerMoveSide == MoveSide.Left &&
                sphereMoveSide[i] == MoveSide.Up &&
                spherePosOnGrid[i].x + 1 == playerPosOnGrid.x &&
                spherePosOnGrid[i].y + 1 == playerPosOnGrid.y)
            {
                sphereSwap[i] = true;
                playerSwap = true;
            }
            else
            {
                playerSwap = false;
                sphereSwap[i] = false;
            }

            if (playerPosOnGrid.y == density.x - 1 && playerMoveSide == MoveSide.Right)
            {
                playerMoveSide = MoveSide.Left;
            }
            else if (playerPosOnGrid.y == 0 && playerMoveSide == MoveSide.Left)
            {
                playerMoveSide = MoveSide.Right;
            }

            if (playerPosOnGrid.x == density.y - 1 && playerMoveSide == MoveSide.Up)
            {
                playerMoveSide = MoveSide.Down;
            }
            else if (playerPosOnGrid.x == 0 && playerMoveSide == MoveSide.Down)
            {
                playerMoveSide = MoveSide.Up;
            }
        }

        // Calculate for player and colors
        for (int i = 0; i < colorList.Count; i++)
        {
            if (playerColor == colorColor[i])
            {
                if (playerMoveSide == MoveSide.Up && playerPosOnGrid.y + 2 == colorPosOnGrid[i].y && playerPosOnGrid.x == colorPosOnGrid[i].x)
                {
                    colorReadyToDestroy[i] = true;
                }
                if (playerMoveSide == MoveSide.Down && playerPosOnGrid.y - 2 == colorPosOnGrid[i].y && playerPosOnGrid.x == colorPosOnGrid[i].x)
                {
                    colorReadyToDestroy[i] = true;
                }
                if (playerMoveSide == MoveSide.Right && playerPosOnGrid.y == colorPosOnGrid[i].y && playerPosOnGrid.x + 2 == colorPosOnGrid[i].x)
                {
                    colorReadyToDestroy[i] = true;
                }
                if (playerMoveSide == MoveSide.Left && playerPosOnGrid.y == colorPosOnGrid[i].y && playerPosOnGrid.x - 2 == colorPosOnGrid[i].x)
                {
                    colorReadyToDestroy[i] = true;
                }
            }
        }
    }

    private void CalculateMovingSpheres(Transform currentSphere)
    {
        // Calculate for sphere
        int mainIdx = sphereList.FindIndex(v => v == currentSphere);
        Vector2 currentSpherePosOnGrid = spherePosOnGrid[mainIdx];
        MoveSide currentSphereMoveSide = sphereMoveSide[mainIdx];
        bool currentSpheresSwap = sphereSwap[mainIdx];

        for (int i = 0; i < spherePosOnGrid.Count; i++)
        {
            if (i == mainIdx) // Don't touch current spheres in List
                continue;

            // Check to сollision avoidance
            if (currentSphereMoveSide == MoveSide.Right && ((sphereMoveSide[i] == MoveSide.Up) || (sphereMoveSide[i] == MoveSide.Down)))
            {
                if (spherePosOnGrid[i].x - 2 == currentSpherePosOnGrid.x)
                {
                    if (spherePosOnGrid[i].y - 2 == currentSpherePosOnGrid.y || spherePosOnGrid[i].y + 2 == currentSpherePosOnGrid.y)
                    {
                        sphereMoveSide[i] = MoveSide.Left;
                    }
                }
            }
            else if (currentSphereMoveSide == MoveSide.Left && ((sphereMoveSide[i] == MoveSide.Up) || (sphereMoveSide[i] == MoveSide.Down)))
            {
                if (spherePosOnGrid[i].x + 2 == currentSpherePosOnGrid.x)
                {
                    if (spherePosOnGrid[i].y - 2 == currentSpherePosOnGrid.y || spherePosOnGrid[i].y + 2 == currentSpherePosOnGrid.y)
                    {
                        sphereMoveSide[i] = MoveSide.Right;
                    }
                }
            }

            // Change side when sphere locate near bounds
            if (currentSpherePosOnGrid.y == density.x - 1 && currentSphereMoveSide == MoveSide.Right)
            {
                sphereMoveSide[i] = MoveSide.Left;
            }
            else if (currentSpherePosOnGrid.y == 0 && currentSphereMoveSide == MoveSide.Left)
            {
                sphereMoveSide[i] = MoveSide.Right;
            }

            if (currentSpherePosOnGrid.x == density.y - 1 && currentSphereMoveSide == MoveSide.Up)
            {
                sphereMoveSide[i] = MoveSide.Down;
            }
            else if (currentSpherePosOnGrid.x == 0 && currentSphereMoveSide == MoveSide.Down)
            {
                sphereMoveSide[i] = MoveSide.Up;
            }

        }

    }

    private void SwipeUp()
    {
        playerMoveSide = MoveSide.Up;

        if (playerPosOnGrid.x == density.y - 1 && playerMoveSide == MoveSide.Up)
        {
            playerMoveSide = MoveSide.Down;
        }
    }

    private void SwipeDown()
    {
        playerMoveSide = MoveSide.Down;

        if (playerPosOnGrid.x == 0 && playerMoveSide == MoveSide.Down)
        {
            playerMoveSide = MoveSide.Up;
        }
    }

    private void SwipeLeft()
    {
        playerMoveSide = MoveSide.Left;

        if (playerPosOnGrid.y == 0 && playerMoveSide == MoveSide.Left)
        {
            playerMoveSide = MoveSide.Right;
        }
    }

    private void SwipeRight()
    {
        playerMoveSide = MoveSide.Right;

        if (playerPosOnGrid.y == density.x - 1 && playerMoveSide == MoveSide.Right)
        {
            playerMoveSide = MoveSide.Left;
        }
    }

    private void Tap()
    {
        Debug.Log("Tap");
    }
}
