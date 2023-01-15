using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityScriptableSettings;

public class PlayerKoboldLoader : MonoBehaviour {
    private static readonly string[] settingNames = {"Hue", "Brightness", "Contrast", "Saturation", "BoobSize", "KoboldSize", "DickSize", "DickThickness", "BallSize"};
    public Kobold targetKobold;
    void Start() {
        foreach(string settingName in settingNames) {
            var option = SettingsManager.GetSetting(settingName);
            if (option is SettingFloat optionFloat) {
                optionFloat.changed -= OnValueChange;
                optionFloat.changed += OnValueChange;
            } else {
                throw new UnityException($"Setting {settingName} is not a SettingFloat");
            }
        }

        var dickOption = SettingsManager.GetSetting("DickWithTypes");
        if (dickOption is SettingInt optionInt) {
            optionInt.changed -= OnValueChange;
            optionInt.changed += OnValueChange;
        } else {
            throw new UnityException($"Setting Dick is not a SettingInt");
        }
        var flipOption = SettingsManager.GetSetting("FlipColors");
        if (flipOption is SettingInt optionInt2)
        {
            optionInt2.changed -= OnValueChange;
            optionInt2.changed += OnValueChange;
        }
        else
        {
            throw new UnityException($"Setting FlipColors is not a SettingInt");
        }

        targetKobold.SetGenes(GetPlayerGenes());
    }
    void OnDestroy() {
        foreach(string settingName in settingNames) {
            var option = SettingsManager.GetSetting(settingName);
            if (option is SettingFloat optionFloat) {
                optionFloat.changed -= OnValueChange;
            }
        }
        var dickOption = SettingsManager.GetSetting("DickWithTypes");
        if (dickOption is SettingInt optionInt) {
            optionInt.changed -= OnValueChange;
        }
        var flipOption = SettingsManager.GetSetting("FlipColors");
        if (flipOption is SettingInt optionInt2)
        {
            optionInt2.changed -= OnValueChange;
        }
    }
    private static KoboldGenes ProcessOption(KoboldGenes genes, SettingInt setting) {
        switch (setting.name) {
            case "DickWithTypes":
				switch (setting.GetValue())
				{
                    default:
                        genes.dickEquip = (byte)255;
                        break;
                    case 1:
                        genes.dickEquip = (byte)0;
                        break;
                    case 2:
                        genes.dickEquip = (byte)2;
                        break;
                    case 3:
                        genes.dickEquip = (byte)13;
                        break;
                    case 4:
                        genes.dickEquip = (byte)14;
                        break;
                    case 5:
                        genes.dickEquip = (byte)3;
                        break;
                    case 6:
                        genes.dickEquip = (byte)1;
                        break;
                    case 7:
                        genes.dickEquip = (byte)4;
                        break;
                }
            break;

            //case "FlipColors":  
            //SettingFloat hueFix = SettingsManager.GetSetting("Hue") as SettingFloat;    //haha... causes an infinite loop by continually altering the hue setting
            //hueFix.SetValue((hueFix.GetValue() + .5f) %1f);
            //break;
                }
        return genes;
    }
    private static KoboldGenes ProcessOption(KoboldGenes genes, SettingFloat setting) {
        switch(setting.name) {
            case "Hue": //genes.hue = (byte)Mathf.RoundToInt(setting.GetValue()*255f); break;
                float newHue = Mathf.RoundToInt(setting.GetValue()*255f);
                SettingInt hueFix = SettingsManager.GetSetting("FlipColors") as SettingInt;
                if(hueFix.GetValue()==1) { newHue = (newHue + 128f) % 255f; }
                genes.hue = (byte)newHue;
                break;
            case "Brightness": genes.brightness = (byte)Mathf.RoundToInt(setting.GetValue()*255f); break;
            //case "Contrast": genes.contrast = (byte)Mathf.RoundToInt((setting.GetValue()+1f)*127.5f); break;       //for contrast bar of -1 to 1
            case "Contrast":    //for contrast bar of 0 to 1 with a flip setting
                SettingInt conSetting = SettingsManager.GetSetting("FlipColors") as SettingInt;
                float flipper = (conSetting.GetValue() * -2f) + 1f;
                genes.contrast = (byte)Mathf.RoundToInt((setting.GetValue() * 127.5f * flipper) + 127.5f);
                break; 
            case "Saturation": genes.saturation = (byte)Mathf.RoundToInt(setting.GetValue()*255f); break;
            case "DickSize": genes.dickSize = Mathf.Lerp(0f, 10f, setting.GetValue()); break;
            case "BallSize": genes.ballSize = Mathf.Lerp(5f, 10f, setting.GetValue()); break;
            case "DickThickness": genes.dickThickness = Mathf.Lerp(0.1f, 1.5f, setting.GetValue()); break;
            case "BoobSize": genes.breastSize = setting.GetValue() * 30f; break;
            case "KoboldSize": genes.baseSize = setting.GetValue() * 20f; break;
        }
        return genes;
    }

    public static KoboldGenes GetPlayerGenes() {
        KoboldGenes genes = new KoboldGenes();
        foreach (string setting in settingNames) {
            genes = ProcessOption(genes, SettingsManager.GetSetting(setting) as SettingFloat);
        }
        genes = ProcessOption(genes, SettingsManager.GetSetting("DickWithTypes") as SettingInt);
        genes = ProcessOption(genes, SettingsManager.GetSetting("FlipColors") as SettingInt);
        return genes;
    }

    void OnValueChange(int newValue) {
        targetKobold.SetGenes(GetPlayerGenes());
    }

    void OnValueChange(float newValue) {
        targetKobold.SetGenes(GetPlayerGenes());
    }
}
