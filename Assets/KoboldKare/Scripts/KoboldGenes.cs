using System;
using NetStack.Quantization;
using NetStack.Serialization;
using Photon.Pun;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityScriptableSettings;

public static class KoboldGenesBitBufferExtension {
    public static void AddKoboldGenes(this BitBuffer buffer, KoboldGenes genes) {
        if (genes == null) {
            buffer.AddBool(false);
            return;
        }
        buffer.AddBool(true);
        ushort maxEnergyQ = HalfPrecision.Quantize(genes.maxEnergy);
        ushort baseSizeQ = HalfPrecision.Quantize(genes.baseSize);
        ushort fatSizeQ = HalfPrecision.Quantize(genes.fatSize);
        ushort ballSizeQ = HalfPrecision.Quantize(genes.ballSize);
        ushort dickSizeQ = HalfPrecision.Quantize(genes.dickSize);
        ushort breastSizeQ = HalfPrecision.Quantize(genes.breastSize);
        ushort bellySizeQ = HalfPrecision.Quantize(genes.bellySize);
        ushort metabolizeCapacitySizeQ = HalfPrecision.Quantize(genes.metabolizeCapacitySize);
        ushort dickThicknessQ = HalfPrecision.Quantize(genes.dickThickness);
        buffer.AddUShort(maxEnergyQ);
        buffer.AddUShort(baseSizeQ);
        buffer.AddUShort(fatSizeQ);
        buffer.AddUShort(ballSizeQ);
        buffer.AddUShort(dickSizeQ);
        buffer.AddUShort(breastSizeQ);
        buffer.AddUShort(bellySizeQ);
        buffer.AddUShort(metabolizeCapacitySizeQ);
        buffer.AddUShort(dickThicknessQ);
        buffer.AddByte(genes.hue);
        buffer.AddByte(genes.brightness);
        buffer.AddByte(genes.saturation);
        buffer.AddByte(genes.contrast);
        buffer.AddByte(genes.dickEquip);
        buffer.AddByte(genes.grabCount);
        buffer.AddByte(genes.maxGrab);

    }
    public static KoboldGenes ReadKoboldGenes(this BitBuffer buffer) {
        if (!buffer.ReadBool()) {
            return null;
        }
        return new KoboldGenes {
            maxEnergy = HalfPrecision.Dequantize(buffer.ReadUShort()),
            baseSize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            fatSize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            ballSize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            dickSize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            breastSize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            bellySize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            metabolizeCapacitySize = HalfPrecision.Dequantize(buffer.ReadUShort()),
            dickThickness = HalfPrecision.Dequantize(buffer.ReadUShort()),
            hue = buffer.ReadByte(),
            brightness = buffer.ReadByte(),
            saturation = buffer.ReadByte(),
            contrast = buffer.ReadByte(),
            dickEquip = buffer.ReadByte(),
            grabCount = buffer.ReadByte(),
            maxGrab = buffer.ReadByte()
        };
    }
}

[System.Serializable]
public class KoboldGenes {
    public float maxEnergy = 5f;
    public float baseSize = 20f;
    public float fatSize;
    public float ballSize;
    public float dickSize;
    public float breastSize;
    public float bellySize = 20f;
    public float metabolizeCapacitySize = 20f;
    public float dickThickness;
    public byte hue;
    public byte brightness = 128;
    public byte saturation = 128;
    public byte contrast = 191;    //currently swaps between .5 and -.5 so as to flip contrast. Effective "value" is -1 to 1
    public byte dickEquip = byte.MaxValue;
    public byte grabCount = 1;
    public byte maxGrab = 1;



    private static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f) {
        float u, v, S;
        do {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            S = u * u + v * v;
        } while (S >= 1.0f);
        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    public KoboldGenes With(float? maxEnergy = null, float? baseSize = null, float? fatSize = null,
            float? ballSize = null, float? dickSize = null, float? breastSize = null, float? bellySize = null,
            float? metabolizeCapacitySize = null, byte? hue = null, byte? brightness = null,
            byte? saturation = null, byte? contrast = null, byte? dickEquip = null, float? dickThickness = null, 
            byte? grabCount = null, byte? maxGrab = null) {
        return new KoboldGenes() {
            maxEnergy = maxEnergy ?? this.maxEnergy,
            baseSize = baseSize ?? this.baseSize,
            fatSize = fatSize ?? this.fatSize,
            ballSize = ballSize ?? this.ballSize,
            dickSize = dickSize ?? this.dickSize,
            breastSize = breastSize ?? this.breastSize,
            bellySize = bellySize ?? this.bellySize,
            metabolizeCapacitySize = metabolizeCapacitySize ?? this.metabolizeCapacitySize,
            hue = hue ?? this.hue,
            brightness = brightness ?? this.brightness,
            saturation = saturation ?? this.saturation,
            contrast = contrast ?? this.contrast,
            dickEquip = dickEquip ?? this.dickEquip,
            dickThickness = dickThickness ?? this.dickThickness,
            grabCount = grabCount ?? this.grabCount,
            maxGrab = maxGrab ?? this.maxGrab
        };
    }

    private byte GetRandomDick() {
        var equipments = EquipmentDatabase.GetEquipments();
        float totalDicks = 0f;
        foreach(var equipment in equipments) {
            if (equipment is DickEquipment) {
                totalDicks += 1f;
            }
        }
        float randomSelection = Random.Range(0f, totalDicks);
        float selection = 0f;
        foreach(var equipment in equipments) {
            if (equipment is not DickEquipment) continue;
            selection += 1f;
            if (!(selection >= randomSelection)) continue;
            return (byte)equipments.IndexOf(equipment);
        }
        return byte.MaxValue;
    }

    public KoboldGenes Randomize(float multiplier=1f) {
        // Slight bias for male kobolds, as they have more variety.
        if (Random.Range(0f,1f) > 0.4f) {
            breastSize = Random.Range(0f, 10f)*multiplier;
            ballSize = Random.Range(10f, 20f)*multiplier;
            dickSize = Random.Range(0f, 20f)*multiplier;
            dickEquip = GetRandomDick();
        } else {
            breastSize = Random.Range(10f, 40f)*multiplier;
            ballSize = Random.Range(5f, 25f)*multiplier;
            dickSize = Random.Range(0f, 20f)*multiplier;
            dickEquip = byte.MaxValue;
        }

        fatSize = Random.Range(0f, 3f);
        dickThickness = RandomGaussian(0f, 1f)*multiplier;
        baseSize = Random.Range(14f, 24f)*multiplier;
        hue = (byte)Random.Range(0, 255);
        brightness = (byte)Mathf.RoundToInt(Mathf.Clamp(RandomGaussian(-0.2f, 1.2f), 0, 1) * 255f);
        saturation = (byte)Mathf.RoundToInt(Mathf.Clamp(RandomGaussian(-0.2f, 1.2f), 0, 1) * 255f);
        float contrastBase = 0.5f;  //locks contrast to either .5 or -.5
                                    //The line below would allow randomized contrast to be added if swapped with the above
                                    //float contrastBase = Mathf.Clamp(RandomGaussian(-0.2f, 1.2f), 0, 1);
        contrast = (byte)Mathf.RoundToInt((Mathf.Lerp(contrastBase * -1, contrastBase, (Random.Range(0f, 1f) > .05f) ? 1f : 0f) * 127.5f) + 127.5f); //5% chance of inverted kobold
        return this;
    }

    public static KoboldGenes Mix(KoboldGenes a, KoboldGenes b) {
        KoboldGenes c;
        // This should never happen.
        if (a == null && b == null) {
            Debug.LogError("Tried to mix two null gene pools, how does this happen?");
            return new KoboldGenes().Randomize(1f);
        }
        
        // Single parent? Also shouldn't happen.
        if (a == null) {
            return b;
        }
        if (b == null) {
            return a;
        }

        if (Random.Range(0f, 1f) > 0.5f) {
            c = (KoboldGenes)a.MemberwiseClone();
        } else {
            c = (KoboldGenes)b.MemberwiseClone();
        }

        SettingFloat variation = SettingsManager.GetSetting("GeneticVariation") as SettingFloat;           //these two edits will allow alternate amounts for genetic variation
        float varMulti = .3f;
        varMulti = variation.GetValue();
        //Debug.Log("setting is " + variation.GetValue());
        // Blend hue, hue is angle-based, so it loops around. 
        float hueAngA = a.hue / 255f;
        float hueAngB = b.hue / 255f;
        float grab1 = Mathf.Lerp(a.maxGrab, a.grabCount, (a.grabCount > a.maxGrab) ? 1f : 0f);  //these two check and update old kobolds to the new grab toggle system
        float grab2 = Mathf.Lerp(b.maxGrab, b.grabCount, (b.grabCount > b.maxGrab) ? 1f : 0f);
        c.hue = (byte)Mathf.RoundToInt(FloatExtensions.CircularLerp(hueAngA, hueAngB, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1)) * 255f);
        c.brightness = (byte)Mathf.RoundToInt(Mathf.Lerp(a.brightness / 255f, b.brightness / 255f, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1)) * 255f);
        c.saturation = (byte)Mathf.RoundToInt(Mathf.Lerp(a.saturation / 255f, b.saturation / 255f, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1)) * 255f);
        //c.contrast = (byte)Mathf.RoundToInt(Mathf.Lerp(a.contrast / 255f, b.contrast / 255f, Mathf.Clamp(RandomGaussian(-0.1f+varMulti, 1.1f-varMulti), 0, 1)) * 255f);
        //if using full random, use ^. Otherwise the memberwise clone will select the contrast randomly between the parents.
        c.bellySize = Mathf.Lerp(a.bellySize, b.bellySize, Mathf.Clamp(RandomGaussian(-0.1f+varMulti, 1.1f-varMulti), 0, 1));
        c.metabolizeCapacitySize = Mathf.Lerp(a.metabolizeCapacitySize, b.metabolizeCapacitySize, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.dickSize = Mathf.Lerp(a.dickSize, b.dickSize, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.ballSize = Mathf.Lerp(a.ballSize, b.ballSize, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.fatSize = Mathf.Lerp(a.fatSize, b.fatSize, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.baseSize = Mathf.Lerp(a.baseSize, b.baseSize, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.maxEnergy = Mathf.Lerp(a.maxEnergy, b.maxEnergy, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.dickThickness = Mathf.Lerp(a.dickThickness, b.dickThickness, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1));
        c.maxGrab = (byte)Mathf.Max(Mathf.RoundToInt(Mathf.Lerp(grab1, grab2, Mathf.Clamp(RandomGaussian(-0.1f + varMulti, 1.1f - varMulti), 0, 1))), 1);
        c.grabCount = c.maxGrab;

        return c;
    }

    public int GrabToggle() //toggles your grab from whatever the max is to 1 and back, returns the new value for the grabber
    {
        //Debug.Log("Kobold Genes: OldGrab- " + grabCount);
        if (grabCount > maxGrab) { maxGrab = grabCount; }  //also updates max grab in case of old kobold. Can be removed later
        if (maxGrab == grabCount) { grabCount = 1; }
        else { grabCount = maxGrab; }
        //Debug.Log("Kobold Genes: NewGrab- " + grabCount);
        //Debug.Log("Kobold Genes: MaxGrab- " + maxGrab);
        return grabCount;
    }

    /*public const short byteCount = sizeof(float) * 9 + sizeof(byte) * 5;
    public static short Serialize(StreamBuffer outStream, object customObject) {
        KoboldGenes genes = (KoboldGenes)customObject;
        byte[] bytes = new byte[byteCount];
        int index = 0;
        Protocol.Serialize(genes.maxEnergy, bytes, ref index);
        Protocol.Serialize(genes.baseSize, bytes, ref index);
        Protocol.Serialize(genes.fatSize, bytes, ref index);
        Protocol.Serialize(genes.ballSize, bytes, ref index);
        Protocol.Serialize(genes.dickSize, bytes, ref index);
        Protocol.Serialize(genes.breastSize, bytes, ref index);
        Protocol.Serialize(genes.bellySize, bytes, ref index);
        Protocol.Serialize(genes.metabolizeCapacitySize, bytes, ref index);
        bytes[index++] = genes.hue;
        bytes[index++] = genes.brightness;
        bytes[index++] = genes.saturation;
        bytes[index++] = genes.contrast;
        bytes[index++] = genes.dickEquip;
        bytes[index++] = genes.grabCount;
        bytes[index++] = genes.maxGrab;

        Protocol.Serialize(genes.dickThickness, bytes, ref index);
        outStream.Write(bytes, 0, byteCount);
        return byteCount;
    }
    public static object Deserialize(StreamBuffer inStream, short length) {
        KoboldGenes genes = new KoboldGenes();
        byte[] bytes = new byte[length];
        inStream.Read(bytes, 0, length);
        int index = 0;
        while (index < length) {
            Protocol.Deserialize(out genes.maxEnergy, bytes, ref index);
            Protocol.Deserialize(out genes.baseSize, bytes, ref index);
            Protocol.Deserialize(out genes.fatSize, bytes, ref index);
            Protocol.Deserialize(out genes.ballSize, bytes, ref index);
            Protocol.Deserialize(out genes.dickSize, bytes, ref index);
            Protocol.Deserialize(out genes.breastSize, bytes, ref index);
            Protocol.Deserialize(out genes.bellySize, bytes, ref index);
            Protocol.Deserialize(out genes.metabolizeCapacitySize, bytes, ref index);
            genes.hue = bytes[index++];
            genes.brightness = bytes[index++];
            genes.saturation = bytes[index++];
            genes.contrast = bytes[index++];
            genes.dickEquip = bytes[index++];
            genes.grabCount = bytes[index++];
            genes.maxGrab = bytes[index++];
            Protocol.Deserialize(out genes.dickThickness, bytes, ref index);
        }
        return genes;
    }*/

    public void Save(JSONNode node, string key) {
        JSONNode rootNode = JSONNode.Parse("{}");
        rootNode["maxEnergy"] = maxEnergy;
        rootNode["baseSize"] = baseSize;
        rootNode["fatSize"] = fatSize;
        rootNode["ballSize"] = ballSize;
        rootNode["dickSize"] = dickSize;
        rootNode["breastSize"] = breastSize;
        rootNode["bellySize"] = bellySize;
        rootNode["metabolizeCapacitySize"] = metabolizeCapacitySize;
        rootNode["hue"] = (int)hue;
        rootNode["brightness"] = (int)brightness;
        rootNode["saturation"] = (int)saturation;
        rootNode["contrast"] = (int)contrast;
        rootNode["dickEquip"] = (int)dickEquip;
        rootNode["grabCount"] = (int)grabCount;
        rootNode["maxGrab"] = (int)maxGrab;
        rootNode["dickThickness"] = dickThickness;
        node[key] = rootNode;
    }

    public void Load(JSONNode node, string key) {
        JSONNode rootNode = node[key];
        maxEnergy = rootNode["maxEnergy"];
        baseSize = rootNode["baseSize"];
        fatSize = rootNode["fatSize"];
        ballSize = rootNode["ballSize"];
        dickSize = rootNode["dickSize"];
        breastSize = rootNode["breastSize"];
        bellySize = rootNode["bellySize"];
        metabolizeCapacitySize = rootNode["metabolizeCapacitySize"];
        hue = (byte)rootNode["hue"].AsInt;
        brightness = (byte)rootNode["brightness"].AsInt;
        saturation = (byte)rootNode["saturation"].AsInt;
        contrast = (byte)rootNode["contrast"].AsInt;
        dickEquip = (byte)rootNode["dickEquip"].AsInt;
        grabCount = (byte)rootNode["grabCount"].AsInt;
        maxGrab = (byte)rootNode["maxGrab"].AsInt;
        if (maxGrab == 0) { contrast = 191; }   //there IS no fixed start value for old kobolds so it defaults to 0. Returns it to ".5"
        dickThickness = rootNode["dickThickness"];
    }

    public override string ToString() {
        return $@"Kobold Genes: 
           maxEnergy: {maxEnergy}
           baseSize: {baseSize}
           fatSize: {fatSize}
           ballSize: {ballSize}
           dickSize: {dickSize}
           breastSize: {breastSize}
           bellySize: {bellySize}
           metabolizeCapacitySize: {metabolizeCapacitySize}
           hue: {hue}
           brightness: {brightness}
           saturation: {saturation}
           contrast: {contrast}
           dickEquip: {dickEquip}
           grabCount: {grabCount}
           maxGrab: {maxGrab}
           dickThickness: {dickThickness}";
    }
}

public class GeneHolder : MonoBehaviourPun {
    private KoboldGenes genes;

    public delegate void GenesChangedAction(KoboldGenes newGenes);

    public event GenesChangedAction genesChanged;

    public KoboldGenes GetGenes() {
        return genes;
    }

    public int ToggleMultiGrab()
    {
        int amount = genes.GrabToggle();
        //Debug.Log("Geneholder: Amount- " + amount);
        return amount;
    }
    public virtual void SetGenes(KoboldGenes newGenes) {
        genes = newGenes;
        genesChanged?.Invoke(newGenes);
    }
}

