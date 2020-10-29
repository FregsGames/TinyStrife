using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static int totalCharacters = 4; //KEEP THIS UPDATED

    //------General-----//
    int act;
    float gameTime;
    bool[] team = new bool[totalCharacters]; //bonny dreshi elektra murray
    //relics
    //shop stuff (skull, potion)

    //-------World--------//
    Vector2 playerPos;
    int[,] worldTypes; // 0: null, 1: start, 2: combat, 3: shop, 4: blacksmith 5: boss
    int[,] worldWeatherTypes; // 0: null  1: field, 2: desert, 3: forest, 4: dark forest, 5: snow, 6: fall, 7: vulcano, 8: water, 9: spooky

    //-----Characters----//
    int[] levels = new int[totalCharacters];
    float[] experience = new float[totalCharacters];
    int[,] stats = new int[totalCharacters, 5]; //hp, energy, str, def, spd
    int[,] _stats = new int[totalCharacters, 5]; //hp, energy, str, def, spd
    bool[,] talents = new bool[totalCharacters, 7]; //left to right 
    int[,] attacks = new int[totalCharacters, 3];

   /* void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            string line = "";
            for (int i = 0; i < stats.GetLength(0); i++)
            {
                for (int j = 0; j < stats.GetLength(1); j++)
                {
                    line += i + "" + j + ": " + stats[i, j] + " ";
                }
                line += "\n";
            }
            Debug.Log(line);
        }
    }*/

    //References
    PathGenerator pGenerator;
    StatisticsEnd statistics;
    GameManager gManager;
    PathSelector pSelector;

    public bool SaveGameExists()
    {
        if (PlayerPrefs.GetInt("s_gameExists", 0) == 1)
            return true;
        return false;
    }

    public IEnumerator LoadData()
    {
        AudioManager.instance.ChangeMusic(AudioManager.ThemeType.nexus1);

        yield return StartCoroutine(LoadScenes());
        Debug.Log("Calling retrieve save data");
        RetrieveSaveData();


        //TODO
        // ---- General ---- // 
        if (pGenerator == null)
            pGenerator = PathGenerator.instance;
        if (pGenerator == null)
            Debug.LogError("No PathGenerator found when trying to load data");

        pGenerator.floor = act;

        if (statistics == null)
            statistics = StatisticsEnd.instance;
        if (statistics == null)
            Debug.LogError("No StatisticsEnd found when trying to load data");

        statistics.SetElapsedTime(gameTime);

        if (gManager == null)
            gManager = GameManager.instance;
        if (gManager == null)
            Debug.LogError("No GameManager found when trying to load data");

        //Team loading
        if(PlayerCharactersUnlockeables.instance == null)
            Debug.LogError("No PlayerCharactersUnlockeables found when trying to load data");

        PlayerCharactersUnlockeables.instance.RetrieveUnlocks();
        int i = 0;
        List<Fighter> fighters = new List<Fighter>();

        foreach (KeyValuePair<GameObject, bool> characters in PlayerCharactersUnlockeables.instance.GetUnlockeables())
        {
            if (team[i])
            {
                fighters.Add(characters.Key.GetComponent<Fighter>());
            }
            i++;
        }
        gManager.teamA = fighters.ToArray();
        gManager.SetupFromSave();

        // World ------//
        pGenerator.GenerateNewWorlds(worldTypes);

        if (pSelector == null)
            pSelector = PathSelector.instance;
        if (pSelector == null)
            Debug.LogError("No PathSelector found when trying to load data");

        pSelector.SetCurrentWorld(playerPos);


        //Characters ---- //
        i = 0;
        foreach (Fighter f in gManager.teamA)
        {
            f.GetComponent<FighterExperience>().SetLevel(levels[f.fUnlockIndex], i + 1);
            f.GetComponent<FighterExperience>().SetCurrentExperience(experience[f.fUnlockIndex]);
            f.hp = stats[f.fUnlockIndex, 0];
            f.str = stats[f.fUnlockIndex, 2];
            f.def = stats[f.fUnlockIndex, 3];
            f.spd = stats[f.fUnlockIndex, 4];
            f.energy = stats[f.fUnlockIndex, 1];

            f._hp = _stats[f.fUnlockIndex, 0];
            f._str = _stats[f.fUnlockIndex, 2];
            f._def = _stats[f.fUnlockIndex, 3];
            f._spd = _stats[f.fUnlockIndex, 4];
            f._energy = _stats[f.fUnlockIndex, 1];

            foreach (KeyValuePair<int, PasiveSkill> skills in f.GetComponentInChildren<SkillTree>().characterSkillTree)
            {
                if(talents[f.fUnlockIndex, skills.Key])
                    skills.Value.SelectSkill();
            }

            int attackLength = 0;
            for (int k = 0; k < attacks.GetLength(1); k++)
            {
                if (attacks[f.fUnlockIndex, k] != -1)
                {
                    attackLength++;
                }
            }

            f.attacks = new Attack[attackLength];
            for (int k = 0; k < f.attacks.Length; k++)
            {
                if (attacks[f.fUnlockIndex, k] != -1)
                {
                    f.attacks[k] = AttackStorage.instance.GetSkills()[attacks[f.fUnlockIndex, k]].GetComponent<Attack>();
                }
            }

            i++;
        }

        SceneManager.UnloadSceneAsync("Loader");
        SceneManager.UnloadSceneAsync("Loading");
        SceneManager.UnloadSceneAsync("Menu");
    }

    IEnumerator LoadScenes()
    {
        AsyncOperation ao;
        ao = SceneManager.LoadSceneAsync("Loader", LoadSceneMode.Additive);
        while (!ao.isDone)
        {
            yield return null;
        }

        SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Additive);

        ao = SceneManager.LoadSceneAsync("Nexus", LoadSceneMode.Additive);
        while (!ao.isDone)
        {
            yield return null;
        }

    }

    public void RetrieveSaveData()
    {
        Debug.Log("Retrieving save data");

        //------General-----//
        act = PlayerPrefs.GetInt("s_act", 1);                   //Act
        gameTime = PlayerPrefs.GetFloat("s_gameTime", 0);       //Game Time
        for (int i = 0; i < team.Length; i++)                   //Team (active characters)
        {
            team[i] = PlayerPrefs.GetInt("s_player" + i, 0) == 1 ? true : false;
        }

        //Relics
        //Potion

        //-------World--------//
        playerPos = new Vector2(                                //Player pos
            PlayerPrefs.GetInt("s_playerPosX", 0),
            PlayerPrefs.GetInt("s_playerPosY", 0));

        //Types
        worldTypes = new int[PlayerPrefs.GetInt("s_worldMatrixSizeX", 0), PlayerPrefs.GetInt("s_worldMatrixSizeY", 0)];
        for (int i = 0; i < worldTypes.GetLength(0); i++)
        {
            for (int j = 0; j < worldTypes.GetLength(1); j++)
            {
                worldTypes[i, j] = PlayerPrefs.GetInt("s_worldMatrixType" + i + j, 0);
            }
        }

        //Weathers

        //-----Characters----//
        levels = new int[totalCharacters];
        for (int i = 0; i < team.Length; i++)                   //Team levels
        {
            levels[i] = PlayerPrefs.GetInt("s_playerLvL" + i, 1);
        }

        stats = new int[totalCharacters, 5];
        for (int i = 0; i < totalCharacters; i++)                   //Team stats
        {
            for (int j = 0; j < 5; j++)                   
            {
                stats[i, j] = PlayerPrefs.GetInt("s_playerStat" + i + j, 0);

            }
        }

        _stats = new int[totalCharacters, 5];
        for (int i = 0; i < totalCharacters; i++)                   //Team _stats
        {
            for (int j = 0; j < 5; j++)                   
            {
                _stats[i, j] = PlayerPrefs.GetInt("s_player_Stat" + i + j, 0);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team _talents
        {
            for (int j = 0; j < 7; j++)
            {
                talents[i, j] = PlayerPrefs.GetInt("s_playerTalents" + i + j, 0) == 0? false : true;
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team attacks
        {
            for (int j = 0; j < 3; j++)
            {
                attacks[i, j] = PlayerPrefs.GetInt("s_playerAttacks" + i + j, 0);
            }
        }

    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("s_gameExists", 1);

        //------General-----//
        PlayerPrefs.SetInt("s_act", act);                   //Act
        PlayerPrefs.SetFloat("s_gameTime", gameTime);       //Game Time
        for (int i = 0; i < team.Length; i++)               //Team (active characters)
        {
             PlayerPrefs.SetInt("s_player" + i, team[i] == true ? 1 : 0) ;
        }

        //Relics
        //Potion

        //-------World--------//
        PlayerPrefs.SetInt("s_playerPosX", (int)playerPos[0]); // Player pos
        PlayerPrefs.SetInt("s_playerPosY", (int)playerPos[1]);


        //Types

        PlayerPrefs.SetInt("s_worldMatrixSizeX", worldTypes.GetLength(0));
        PlayerPrefs.SetInt("s_worldMatrixSizeY", worldTypes.GetLength(1));

        for (int i = 0; i < worldTypes.GetLength(0); i++)
        {
            for (int j = 0; j < worldTypes.GetLength(1); j++)
            {
                PlayerPrefs.SetInt("s_worldMatrixType" + i + j, worldTypes[i, j]);
            }
        }

        //Weathers

        //-----Characters----//
        for (int i = 0; i < team.Length; i++)                   //Team levels
        {
            PlayerPrefs.SetInt("s_playerLvL" + i, levels[i]);
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team stats
        {
            for (int j = 0; j < 5; j++)
            {
                PlayerPrefs.SetInt("s_playerStat" + i + j, stats[i, j]);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team _stats
        {
            for (int j = 0; j < 5; j++)
            {
                PlayerPrefs.SetInt("s_player_Stat" + i + j, _stats[i, j]);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team _talents
        {
            for (int j = 0; j < 7; j++)
            {
                PlayerPrefs.SetInt("s_playerTalents" + i + j, talents[i, j] == true? 1 : 0);
            }
        }

        for (int i = 0; i < totalCharacters; i++)                   //Team attacks
        {
            for (int j = 0; j < 3; j++)
            {
                PlayerPrefs.SetInt("s_playerAttacks" + i + j, attacks[i, j]);
            }
        }

    }

    public void StoreData()
    {
        #region storeGeneral
        // ---- General ---- // 
        if (pGenerator == null)
            pGenerator = PathGenerator.instance;
        if (pGenerator == null)
            Debug.LogError("No PathGenerator found when trying to store data");

        act = pGenerator.floor;

        if (statistics == null)
            statistics = StatisticsEnd.instance;
        if (statistics == null)
            Debug.LogError("No StatisticsEnd found when trying to store data");

        gameTime = statistics.GetElapsedTime();

        if (gManager == null)
            gManager = GameManager.instance;
        if (gManager == null)
            Debug.LogError("No GameManager found when trying to store data");

        team = new bool[totalCharacters]; //Set all to false
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            team[gManager.teamA[i].fUnlockIndex] = true;
        }
        #endregion

        #region storeWorld

        // ---- General ---- // 
        if (pSelector == null)
            pSelector = PathSelector.instance;
        if (pSelector == null)
            Debug.LogError("No PathSelector found when trying to store data");

        playerPos = pSelector.currentWorld.matrixIndex;

        worldTypes = new int[pGenerator.worldMatrix.GetLength(0), pGenerator.worldMatrix.GetLength(1)];
        worldWeatherTypes = new int[pGenerator.worldMatrix.GetLength(0), pGenerator.worldMatrix.GetLength(1)];

        for (int i = 0; i < pGenerator.worldMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < pGenerator.worldMatrix.GetLength(1); j++)
            {
                if (pGenerator.worldMatrix[i, j] == null)
                {
                    worldTypes[i, j] = 0;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.start)
                {
                    worldTypes[i, j] = 1;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.normalCombat)
                {
                    worldTypes[i, j] = 2;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.shop)
                {
                    worldTypes[i, j] = 3;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.blacksmith)
                {
                    worldTypes[i, j] = 4;
                    continue;
                }

                if (pGenerator.worldMatrix[i, j].worldType == WorldEvent.WorldType.boss)
                {
                    worldTypes[i, j] = 5;
                    continue;
                }
            }
        }

        //WEATHER TYPES

        #endregion

        #region storeCharacters

        levels = new int[totalCharacters]; //Set all to 0
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            levels[gManager.teamA[i].fUnlockIndex] = gManager.teamA[i].GetComponent<FighterExperience>().GetLevel();
        }

        stats = new int[totalCharacters, 5]; //Set all to 0 //hp, energy, str, def, spd
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            stats[gManager.teamA[i].fUnlockIndex, 0] = gManager.teamA[i].hp;
            stats[gManager.teamA[i].fUnlockIndex, 1] = gManager.teamA[i].energy;
            stats[gManager.teamA[i].fUnlockIndex, 2] = gManager.teamA[i].str;
            stats[gManager.teamA[i].fUnlockIndex, 3] = gManager.teamA[i].def;
            stats[gManager.teamA[i].fUnlockIndex, 4] = gManager.teamA[i].spd;
        }

        _stats = new int[totalCharacters, 5]; //Set all to 0 //hp, energy, str, def, spd
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            _stats[gManager.teamA[i].fUnlockIndex, 0] = gManager.teamA[i]._hp;
            _stats[gManager.teamA[i].fUnlockIndex, 1] = gManager.teamA[i]._energy;
            _stats[gManager.teamA[i].fUnlockIndex, 2] = gManager.teamA[i]._str;
            _stats[gManager.teamA[i].fUnlockIndex, 3] = gManager.teamA[i]._def;
            _stats[gManager.teamA[i].fUnlockIndex, 4] = gManager.teamA[i]._spd;
        }

        experience = new float[totalCharacters]; //Set all to 0
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            experience[gManager.teamA[i].fUnlockIndex] = gManager.teamA[i].GetComponent<FighterExperience>().GetCurrentExp();
        }

        talents = new bool[totalCharacters, 7]; //Set all to false
        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            foreach (KeyValuePair<int, PasiveSkill> skills in gManager.teamA[i].GetComponentInChildren<SkillTree>().characterSkillTree)
            {
                talents[gManager.teamA[i].fUnlockIndex, skills.Key] = skills.Value.IsSelected();
            }
        }

        attacks = new int[totalCharacters, 3];

        for (int i = 0; i < attacks.GetLength(0); i++)
        {
            for (int j = 0; j < attacks.GetLength(1); j++)
            {
                attacks[i, j] = -1;
            }
        }

        for (int i = 0; i < gManager.teamA.Length; i++)
        {
            for (int j = 0; j < gManager.teamA[i].attacks.Length; j++)
            {
                attacks[gManager.teamA[i].fUnlockIndex, j] = gManager.teamA[i].attacks[j] == null ? -1 : gManager.teamA[i].attacks[j].generalIndex;
            }
        }

        #endregion
    }

    public void DeleteSave()
    {
        PlayerPrefs.SetInt("s_gameExists", 0);
    }


    #region singleton
    //Singleton
    public static SaveManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }
    #endregion
}
