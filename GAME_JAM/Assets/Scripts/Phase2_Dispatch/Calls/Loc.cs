using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Basit yerelleştirme: anahtar = İngilizce metnin kendisi. Dil 0 = English, 1 = Türkçe
    /// (SettingsUI ile aynı PlayerPrefs anahtarı). Çevirisi olmayan metin İngilizce kalır.
    /// </summary>
    public static class Loc
    {
        private const string PrefLanguage = "ZH_Language";
        private static int language = -1;

        public static event Action Changed;

        public static int Language
        {
            get
            {
                if (language < 0) language = PlayerPrefs.GetInt(PrefLanguage, 0);
                return language;
            }
        }

        public static bool Turkish => Language == 1;

        public static void SetLanguage(int index)
        {
            if (index == Language) return;
            language = index;
            PlayerPrefs.SetInt(PrefLanguage, index);
            Changed?.Invoke();
        }

        public static string T(string english)
        {
            if (!Turkish || string.IsNullOrEmpty(english)) return english;
            return Tr.TryGetValue(english, out string tr) ? tr : english;
        }

        public static bool Has(string english) => !string.IsNullOrEmpty(english) && Tr.ContainsKey(english);

        public static string F(string englishFormat, params object[] args)
        {
            return string.Format(T(englishFormat), args);
        }

        private static readonly Dictionary<string, string> Tr = new Dictionary<string, string>
        {
            // HUD
            { "UNITS", "BİRİMLER" },
            { "Police", "Polis" },
            { "Ambulance", "Ambulans" },
            { "Fire Truck", "İtfaiye" },
            { "Helicopter", "Helikopter" },
            { "NIGHT SHIFT", "GECE VARDİYASI" },

            // Çağrı kartı
            { "INCOMING CALL", "GELEN ÇAĞRI" },
            { "DECLINE", "REDDET" },
            { "ACCEPT", "KABUL" },
            { "ON CALL", "GÖRÜŞMEDE" },
            { "Caller is speaking...", "Arayan konuşuyor..." },

            // Kaza Dosyası
            { "DAMAGE CONTROL LINE · INCIDENT FILE", "AFET MÜDAHALE HATTI · OLAY DOSYASI" },
            { "DISASTER RESPONSE LINE · INCIDENT FILE", "AFET MÜDAHALE HATTI · OLAY DOSYASI" },
            { "CASE #{0:0000}", "VAKA #{0:0000}" },
            { "INCIDENT", "OLAY" },
            { "ADDRESS", "ADRES" },
            { "AFFECTED", "ETKİLENEN" },
            { "NOTES", "NOTLAR" },
            { "UNIT", "BİRİM" },
            { "FREE", "BOŞTA" },
            { "SEND", "GÖNDER" },
            { "DISMISS CALL", "ÇAĞRIYI KAPAT" },
            { "DISPATCH", "SEVK ET" },
            { "URGENT", "ACİL" },
            { "ROUTINE", "RUTİN" },
            { "Call {0} · Line {1}", "Çağrı {0} · Hat {1}" },
            { " · {0}s left", " · {0} sn kaldı" },
            { " · UNITS EN ROUTE", " · EKİP YOLDA" },
            { " · UNITS ON SCENE", " · EKİP OLAY YERİNDE" },
            { "{0} people · {1} injured", "{0} kişi · {1} yaralı" },
            { "Possible units: ", "Olası ekipler: " },

            // Rozet
            { "EN ROUTE", "YOLDA" },
            { "WORK", "İŞ" },

            // Afet başlıkları
            { "Residential fire", "Konut yangını" },
            { "Earthquake collapse", "Deprem göçüğü" },
            { "Gas explosion", "Doğalgaz patlaması" },
            { "Flash flood", "Ani sel" },
            { "Looting & unrest", "Yağma ve kargaşa" },

            // Afet notları
            { "Gas shut-off valve was never installed. Fire may spread to the house next door.", "Gaz kesme vanası hiç takılmamış. Yangın yan binaya sıçrayabilir." },
            { "Kitchen fire spreading upward. Residents smell gas on the stairs.", "Mutfak yangını üst katlara yayılıyor. Merdivenlerde gaz kokusu var." },
            { "Electrical panel caught fire in the basement. Smoke on every floor.", "Bodrumdaki elektrik panosu tutuştu. Her katta duman var." },
            { "Cracked columns were reported at inspection. Building partly collapsed, people trapped.", "Denetimde çatlak kolonlar raporlanmıştı. Bina kısmen çöktü, mahsur kalanlar var." },
            { "Unsecured shelves and wardrobes fell over. Residents trapped inside.", "Sabitlenmemiş raf ve dolaplar devrildi. Sakinler içeride mahsur." },
            { "A load-bearing column was removed downstairs. Upper floors came down.", "Alt kattaki taşıyıcı kolon kesilmiş. Üst katlar çöktü." },
            { "Faulty gas valve passed inspection. Explosion on the ground floor, fire spreading.", "Arızalı gaz vanası denetimden geçmiş. Zemin katta patlama, yangın yayılıyor." },
            { "Residents reported a gas smell for days. The boiler room exploded.", "Sakinler günlerdir gaz kokusu bildiriyordu. Kazan dairesi patladı." },
            { "Illegal gas line extension ruptured. Windows blown out on three floors.", "Kaçak gaz hattı patladı. Üç kattaki camlar kırıldı." },
            { "Building sits on a filled-in stream bed. Ground floor flooded, families on the roof.", "Bina doldurulmuş dere yatağında. Zemin kat sular altında, aileler çatıda." },
            { "Blocked storm drains, no sandbags. Water rising fast around the entrance.", "Yağmur gideri tıkalı, kum torbası yok. Giriş çevresinde su hızla yükseliyor." },
            { "Basement apartments flooded. Elderly residents cannot reach the stairs.", "Bodrum daireleri su bastı. Yaşlı sakinler merdivenlere ulaşamıyor." },
            { "Shops looted after the tremor. Crowd blocking the street, people injured.", "Sarsıntıdan sonra dükkanlar yağmalandı. Kalabalık sokağı kapattı, yaralılar var." },
            { "Panic at the shelter entrance. Fights breaking out, barricade on fire.", "Toplanma alanı girişinde panik. Kavga çıkıyor, barikat yanıyor." },
            { "Store alarm ringing, windows smashed. Residents afraid to leave the building.", "Dükkan alarmı çalıyor, camlar kırık. Sakinler binadan çıkmaya korkuyor." },

            // Takip ihbarı (deprem / sel sonrası yağma; bir kısmı asılsız)
            { "Looting report (unverified)", "Yağma ihbarı (doğrulanmamış)" },
            { "UNVERIFIED", "TEYİTSİZ" },
            { "Caller claims {0} injured", "Arayan {0} yaralı olduğunu söylüyor" },
            { "Neighbors confirm smashed shop windows. A second caller reports the same street.", "Komşular kırık vitrinleri doğruluyor. İkinci bir arayan da aynı sokağı bildirdi." },
            { "Store owner on the line, alarm clearly audible. Two people hurt by looters.", "Dükkan sahibi hatta, alarm net duyuluyor. Yağmacılar iki kişiyi yaraladı." },
            { "Residents of the damaged building report strangers carrying out their belongings.", "Hasarlı binanın sakinleri, yabancıların eşyalarını taşıdığını bildiriyor." },
            { "Caller refused to give a name and hung up. No other reports from the area.", "Arayan adını vermedi ve kapattı. Bölgeden başka ihbar yok." },
            { "Laughter in the background. The caller changed the address twice.", "Arka planda gülüşmeler. Arayan adresi iki kez değiştirdi." },
            { "Anonymous text message only. Callback number does not exist.", "Sadece isimsiz mesaj. Geri arama numarası mevcut değil." },
            { "FALSE ALARM", "ASILSIZ İHBAR" },
            { "LOOTING CONFIRMED", "YAĞMA DOĞRULANDI" },

            // Telefon mesajları (sağ alt)
            { "MESSAGES", "MESAJLAR" },
            { "Case #{0}: first unit on scene", "Vaka #{0}: ilk ekip olay yerinde" },
            { "Case #{0}: looting confirmed · {1} injured", "Vaka #{0}: yağma doğrulandı · {1} yaralı" },
            { "Case #{0}: {1} lost so far, {2} still trapped", "Vaka #{0}: şu ana kadar {1} kayıp, {2} kişi hâlâ mahsur" },
            { "Case #{0}: no survivors left · {1} lost", "Vaka #{0}: kurtarılacak kimse kalmadı · {1} kayıp" },
            { "Case #{0}: helicopter out of fuel, returning to base", "Vaka #{0}: helikopterin yakıtı bitti, üsse dönüyor" },
            { "Case #{0}: report expired, nobody called back", "Vaka #{0}: ihbar düştü, kimse geri aramadı" },
            { "Case #{0}: time ran out · {1} lost", "Vaka #{0}: süre doldu · {1} kayıp" },
            { "Case #{0}: FALSE ALARM · units returning", "Vaka #{0}: ASILSIZ İHBAR · ekip dönüyor" },
            { "Case #{0} resolved · {1} rescued, {2} lost", "Vaka #{0} çözüldü · {1} kurtarıldı, {2} kayıp" },
            { "Case #{0}: wrong team, no {1} sent · {2} more lost", "Vaka #{0}: yanlış ekip, {1} gitmedi · {2} kayıp daha" },

            // Ayarlar
            { "SETTINGS", "AYARLAR" },
            { "Sound", "Ses" },
            { "Music", "Müzik" },
            { "Language", "Dil" },
            { "Fullscreen", "Tam Ekran" },
            { "DEFAULTS", "VARSAYILAN" },
            { "BACK", "GERİ" },
            { "OFF", "KAPALI" },
            { "ON", "AÇIK" },

            // İpuçları
            { "Answer incoming calls (bottom left). Each call puts a marker on the map.", "Gelen çağrıları aç (sol alt). Her çağrı haritaya bir rozet koyar." },
            { "Click the marker on the map to open the incident file.", "Olay dosyasını açmak için haritadaki rozete tıkla." },
            { "Choose units with [+] (see 'Possible units') and press DISPATCH.", "[+] ile ekip seç ('Olası ekipler'e bak) ve SEVK ET'e bas." },
            { "The yellow ring must close before the white one runs out. Helicopters arrive first!", "Sarı çember, beyaz çember bitmeden kapanmalı. Helikopter herkesten önce varır!" },

            // Vardiya sonu
            { "SHIFT OVER · 05:00 AM", "VARDİYA BİTTİ · 05:00" },
            { "GRADE", "NOT" },
            { "PLAY AGAIN", "TEKRAR OYNA" },
            { "Survivors", "Hayatta" },
            { "Rescued", "Kurtarılan" },
            { "Lost", "Kayıp" },
            { "Survival", "Sağ kalım" },
        };
    }
}
