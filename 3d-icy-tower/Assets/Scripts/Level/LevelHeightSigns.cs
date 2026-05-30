using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LevelHeightSigns : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Milestone Settings")]
    [SerializeField] private float milestoneInterval = 500f;
    [SerializeField] private GameObject milestonePrefab;  // UI deðil 3D TMP prefab
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 0f); // pozisyon ayarý

    private float nextMilestone;

    LevelManager levelManager;

    private void Awake()
    {
        levelManager = GetComponent<LevelManager>();
    }
    private void Start()
    {
        nextMilestone = milestoneInterval;

        // LevelManager'ýn maxHeight'ýna göre tüm milestone'larý baþta spawn et
        SpawnAllMilestones();
    }

    
    private void SpawnAllMilestones()
    {
        float maxHeight = levelManager.NextLevelHeight;

        for (float height = milestoneInterval; height <= maxHeight; height += milestoneInterval)
        {
            Vector3 spawnPos = new Vector3(
                0,
                height,      
                0
            ) + spawnOffset;

            GameObject instance = Instantiate(milestonePrefab, spawnPos, milestonePrefab.transform.rotation);
            TextMeshPro textMesh = instance.GetComponent<TextMeshPro>();

            if (textMesh != null)
                textMesh.text = height.ToString("F0");
        }
    }
    
}
