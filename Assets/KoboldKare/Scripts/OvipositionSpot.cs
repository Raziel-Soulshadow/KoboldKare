using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NetStack.Serialization;
using PenetrationTech;
using Photon.Pun;
using UnityEngine;
using Vilar.AnimationStation;

public class OvipositionSpot : GenericUsable, IAnimationStationSet {
    public delegate void OvipositionAction(int koboldID, int eggID);
    public static event OvipositionAction oviposition;
        
    [SerializeField]
    private Sprite useSprite;
    [SerializeField]
    private AnimationStation station;
    [SerializeField]
    private PhotonGameObjectReference eggPrefab;

    private ScriptableReagent egg;
    private ReadOnlyCollection<AnimationStation> readOnlyStations;

    public override Sprite GetSprite(Kobold k) {
        return useSprite;
    }
    
    public override bool CanUse(Kobold k) {
        return k.bellyContainer.GetVolumeOf(egg) > 5f && station.info.user == null && k.GetEnergy() >= 1f;
    }

    public override void LocalUse(Kobold k) {
        base.LocalUse(k);
        k.photonView.RPC(nameof(CharacterControllerAnimator.BeginAnimationRPC), RpcTarget.All, photonView.ViewID, 0);
    }

    public override void Use() {
        StartCoroutine(EggLayingRoutine());
    }

    void Start() {
        List<AnimationStation> stations = new List<AnimationStation>();
        stations.Add(station);
        readOnlyStations = stations.AsReadOnly();
        egg = ReagentDatabase.GetReagent("Egg");
    }

    IEnumerator EggLayingRoutine() {
        yield return new WaitForSeconds(6f);
        Kobold k = station.info.user;
        if (k == null || !k.photonView.IsMine) {
            yield break;
        }
        
        if (!k.TryConsumeEnergy(1f)) {
            yield break;
        }
        // Find a working penetrable that is enabled, prefer vaginas if we have em.
        Penetrable targetPenetrable = null;
        foreach (var penetrable in k.penetratables) {
            if (penetrable.isFemaleExclusiveAnatomy && penetrable.penetratable.isActiveAndEnabled) {
                targetPenetrable = penetrable.penetratable;
            }
        }

        if (targetPenetrable == null) {
            foreach (var penetrable in k.penetratables) {
                if (penetrable.penetratable.isActiveAndEnabled && penetrable.isFemaleExclusiveAnatomy && penetrable.canLayEgg) {
                    targetPenetrable = penetrable.penetratable;
                }
            }
        }
        
        if (targetPenetrable == null) {
            foreach (var penetrable in k.penetratables) {
                if (penetrable.penetratable.isActiveAndEnabled && penetrable.canLayEgg) {
                    targetPenetrable = penetrable.penetratable;
                }
            }
        }

        float eggVolume = k.bellyContainer.GetVolumeOf(egg);
        k.bellyContainer.OverrideReagent(egg, 0f);

        if (targetPenetrable == null) {
            Debug.LogError("Kobold without a hole tried to make an egg!");
            yield break;
        }

        CatmullSpline path = targetPenetrable.GetSplinePath();
        KoboldGenes mixedGenes = KoboldGenes.Mix(k.GetComponent<Kobold>().GetGenes(),k.bellyContainer.GetGenes());
        BitBuffer spawnData = new BitBuffer(16);
        spawnData.AddKoboldGenes(mixedGenes);
        Penetrator d = PhotonNetwork.Instantiate(eggPrefab.photonName,path.GetPositionFromT(0f), Quaternion.LookRotation(path.GetVelocityFromT(0f).normalized,Vector3.up), 0, new object[]{spawnData}).GetComponentInChildren<Penetrator>();
        if (d == null) {
            yield break;
        }

        ReagentContents eggContents = new ReagentContents();
        eggContents.AddMix(ReagentDatabase.GetReagent("ScrambledEgg").GetReagent(eggVolume));
        
        BitBuffer buffer = new BitBuffer(4);
        buffer.AddReagentContents(eggContents);
        PhotonView eggView = d.gameObject.GetPhotonView();
        eggView.RPC(nameof(GenericReagentContainer.ForceMixRPC), RpcTarget.All, buffer, k.photonView.ViewID, (byte)GenericReagentContainer.InjectType.Inject);

        oviposition?.Invoke(k.photonView.ViewID, eggView.ViewID);
        Rigidbody body = d.GetComponentInChildren<Rigidbody>();
        //d.GetComponent<GenericReagentContainer>().OverrideReagent(ReagentDatabase.GetReagent("ScrambledEgg"), eggVolume);
        body.isKinematic = true;
        // Manually control penetration parameters
        d.Penetrate(targetPenetrable);
        float pushAmount = 0f;
        while (pushAmount < d.GetWorldLength()) {
            CatmullSpline p = targetPenetrable.GetSplinePath();
            Vector3 position = p.GetPositionFromT(0f);
            Vector3 tangent = p.GetVelocityFromT(0f).normalized;
            pushAmount += Time.deltaTime*0.15f;
            body.transform.position = position - tangent * pushAmount;
            yield return null;
        }
        body.isKinematic = false;
    }

    public ReadOnlyCollection<AnimationStation> GetAnimationStations() {
        return readOnlyStations;
    }

    private void OnValidate() {
        eggPrefab.OnValidate();
    }
}
