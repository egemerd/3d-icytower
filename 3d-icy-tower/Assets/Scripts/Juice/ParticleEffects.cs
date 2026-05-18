using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// 1. ADD YOUR PARTICLE NAMES HERE
public enum ParticleType
{
    HitEffect,
    WalkDust,
    PlayerHit,
    WallBounce,
    HitEffect2
}

// Struct to configure particles in the Inspector
[Serializable]
public struct ParticleSetup
{
    public ParticleType type;
    public ParticleSystem prefab;
    public int initialPoolSize;
}

public class ParticleEffects : MonoBehaviour
{
    public static ParticleEffects Instance { get; private set; }

    [Header("Particle Configuration")]
    [SerializeField] private List<ParticleSetup> particleSetups;

    // Object Pool Dictionaries
    private Dictionary<ParticleType, Queue<ParticleSystem>> particlePools;
    private Dictionary<ParticleType, ParticleSystem> particlePrefabs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        particlePools = new Dictionary<ParticleType, Queue<ParticleSystem>>();
        particlePrefabs = new Dictionary<ParticleType, ParticleSystem>();

        // Create a root parent to keep the hierarchy clean
        Transform poolRoot = new GameObject("Particle_Pools").transform;
        poolRoot.SetParent(transform);

        foreach (var setup in particleSetups)
        {
            particlePrefabs.Add(setup.type, setup.prefab);
            Queue<ParticleSystem> newPool = new Queue<ParticleSystem>();

            // Create a specific folder for each particle type
            Transform typeRoot = new GameObject($"{setup.type}_Pool").transform;
            typeRoot.SetParent(poolRoot);

            for (int i = 0; i < setup.initialPoolSize; i++)
            {
                ParticleSystem ps = Instantiate(setup.prefab, typeRoot);
                ps.gameObject.SetActive(false);
                newPool.Enqueue(ps);
            }

            particlePools.Add(setup.type, newPool);
        }
    }

    /// <summary>
    /// Gets a particle from the pool or creates a new one if the pool is empty.
    /// </summary>
    private ParticleSystem GetParticle(ParticleType type)
    {
        if (!particlePools.ContainsKey(type))
        {
            Debug.LogError($"ParticleType {type} does not exist in the pool!");
            return null;
        }

        if (particlePools[type].Count > 0)
        {
            ParticleSystem ps = particlePools[type].Dequeue();
            ps.gameObject.SetActive(true);
            return ps;
        }
        else
        {
            // Pool is empty, expand it dynamically
            ParticleSystem ps = Instantiate(particlePrefabs[type], transform.Find("Particle_Pools").Find($"{type}_Pool"));
            ps.gameObject.SetActive(true);
            return ps;
        }
    }

    /// <summary>
    /// Returns a particle back to its pool array.
    /// </summary>
    private void ReturnToPool(ParticleSystem ps, ParticleType type)
    {
        ps.gameObject.SetActive(false);
        ps.transform.SetParent(transform.Find("Particle_Pools").Find($"{type}_Pool"));
        particlePools[type].Enqueue(ps);
    }

    // ========================================================================
    // ONE-SHOT PARTICLES (Fire and Forget)
    // ========================================================================

    /// <summary>
    /// Plays a particle once and automatically returns it to the pool when it finishes.
    /// </summary>
    public void PlayOneShot(ParticleType type, Vector3 position, Quaternion rotation = default)
    {
        ParticleSystem ps = GetParticle(type);
        if (ps == null) return;

        ps.transform.position = position;
        //ps.transform.rotation = rotation == default ? Quaternion.identity : rotation;
        ps.Play();

        StartCoroutine(ReturnWhenFinished(ps, type));
    }

    public void PlayOneShot(ParticleType type, Transform parent, Vector3 localOffset = default)
    {
        ParticleSystem ps = GetParticle(type);
        if (ps == null) return;

        ps.transform.SetParent(parent);
        ps.transform.position = localOffset;
        //ps.transform.rotation = rotation == default ? Quaternion.identity : rotation;
        ps.Play();

        StartCoroutine(ReturnWhenFinished(ps, type));
    }

    // ========================================================================
    // LOOPING PARTICLES (Toggled by Input/States)
    // ========================================================================

    /// <summary>
    /// Starts a looping particle effect. Attach it to a parent (like the player) so it follows them.
    /// Returns the ParticleSystem reference so you can stop it later.
    /// </summary>
    public ParticleSystem PlayLooping(ParticleType type, Transform parent, Vector3 localOffset = default)
    {
        ParticleSystem ps = GetParticle(type);
        if (ps == null) return null;

        ps.transform.SetParent(parent);
        ps.transform.localPosition = localOffset;
        ps.transform.localRotation = Quaternion.identity;
        ps.Play();

        return ps;
    }

    /// <summary>
    /// Stops a looping particle smoothly (lets existing particles fade out) and returns it to the pool.
    /// </summary>
    public void StopLooping(ParticleSystem ps, ParticleType type)
    {
        if (ps == null) return;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); // Stops generating NEW particles
        StartCoroutine(ReturnWhenFinished(ps, type)); // Waits for the current particles to fade naturally
    }

    // ========================================================================
    // UTILITIES
    // ========================================================================

    /// <summary>
    /// Waits intelligently for a particle to fully die off before tossing it back into the pool.
    /// </summary>
    private IEnumerator ReturnWhenFinished(ParticleSystem ps, ParticleType type)
    {
        // Wait until every single particle has faded away/died
        yield return new WaitWhile(() => ps.IsAlive(true));
        
        ReturnToPool(ps, type);
    }
}
