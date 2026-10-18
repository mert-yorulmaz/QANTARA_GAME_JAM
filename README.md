# 🏢 SIGN (Disaster Control Line)

## 📌 Oyun Hakkında
**SIGN**, iki ana fazdan oluşan, bürokrasi simülasyonu ile kriz ve kaynak yönetimini birleştiren gerilim dolu bir oyundur. Oyuncular, Yapı Denetim ve Kriz Masası Memuru **Deniz** rolünü üstlenirler. 

Gündüz vardiyasında masa başında binaların denetim dosyalarını inceleyip **APPROVE** (Onay) veya **REJECT** (Red) kararları verirken; gece vardiyasında ise "Damage Control Line" kriz masasına geçerek şehirde patlak veren afetlere karşı kısıtlı acil durum birimlerini yönetirler. Gündüz alınan bürokratik kararların ve rüşvet/ihmal tercihlerinin sonuçları, gece şehirdeki yıkım ve can kaybı miktarı olarak doğrudan oyuncunun karşısına çıkar.

---

## 📸 Oyun İçi Görseller ve Oynanış Adımları

### 1. Ana Menü (Main Menu)
![Main Menu](./GameImages/Main%20Menu.jpg)

Oyunun karşılama ve başlangıç ekranı. Üst bantta şehir genelinden anlık canlı haber geçişleri yer alırken, alt HUD panelinde mevcut açık hatlar, hazırda bekleyen birimler ve şehir nüfusu görüntülenmektedir. Oyuncu **START SHIFT** butonuna basarak vardiyasına başlar.

---

### 2. Phase 1: Yapı Denetim Ofisi (Day Shift)
![Phase 1 Day Shift](./GameImages/Part_1jpg.jpg)

Gündüz vardiyasında Deniz, tuğla duvarlı, klostrofobik yapı denetim ofisinde masa başına geçer. Ekranın sol üst köşesindeki **TASKS** görev listesi adımları yönlendirirken, bilgisayar masasına oturarak bina inceleme işlemine geçilir.

---

### 3. Dosya İnceleme ve X-Ray Taraması (Inspect The File)
![Inspect The File](./GameImages/Part_2.jpg)

Bilgisayar ekranında gelen bina denetim dosyası detaylıca incelenir. Dosyadaki yazılı beyanlar kontrol edilirken, **X-RAY SCAN** mekanizması sayesinde binanın 3D tel kafes (wireframe) modeli incelenerek gizli kolon çatlakları, temel kaymaları veya taşıyıcı sistem hasarları tespit edilir.

---

### 4. Yönetmelik Kitabı (The Rule Book)
![The Rule Book](./GameImages/Part_3.jpg)

Dosyayı onaylamadan veya reddetmeden önce masadaki **Rule Book** (Kural Kitabı) açılır. Kitapta yer alan genel evrak kuralları, taşıyıcı sistem şartları, acil durum ekipmanı zorunlulukları ve rüşvet/etik kuralları tek tek kontrol edilerek kararlar bu kurallara göre verilir.

---

### 5. Gece Ekibini Kurma (Build Your Night Crew)
![Build Your Night Crew](./GameImages/Part_4.jpg)

Gündüz vardiyasının sonunda, onaylanan ve reddedilen dosyalardan kazanılan bütçe ile gece vardiyasında görev yapacak acil durum ekibi (**Night Crew**) satın alınır. Polis, Ambulans, İtfaiye ve Helikopter birimleri arasında bütçe dengesi kurularak geceye hazırlık yapılır.

---

### 6. Phase 2: Kriz Masası (Night Shift)
![Phase 2 Night Shift](./GameImages/Part_5.jpg)

Afet gerçekleştiğinde 3D izometrik şehir haritası açılır. Zaman hızla akarken şehir nüfusu (150 resident) takip edilir. Sol üstte zaman ve birim sayaçları, sağ altta ise gelen ihbar mesajları görüntülenir. Her saniye hayati önem taşır.

---

### 7. Olay Dosyası ve Yönlendirme (The Incident File)
![The Incident File](./GameImages/Part_6.jpg)

Şehrin farklı noktalarından gelen ihbar hatlarına tıklanarak olay dosyası açılır. Olayın türüne (deprem enkazı, yangın, sel vb.) ve yaralı durumuna göre eldeki Polis, Ambulans, İtfaiye ve Helikopter birimleri seçilerek olay yerine **DISPATCH** edilir.

---

### 8. Saha Harekatı ve Müdahale (Units Rolling)
![Units Rolling](./GameImages/Part_7.jpg)

Yönlendirilen acil durum araçları sirenlerini açarak şehrin gerçek yol ağını kullanıp olay yerine intikal eder. Gündüz vardiyasında hatalı/rüşvetle onaylanan binalar Phase 2'de ağır enkazlara dönüşür ve bu durum müdahale süresini kısıtlayarak can kayıplarını artırır.
