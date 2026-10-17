DISPATCH - UI ASSET PAKETI
==========================

Tum gorseller 1280x720 tasarim cozunurlugune gore, 2 kat buyuklukte (2560x1440) disa aktarildi.
Unity'de Canvas Scaler: "Scale With Screen Size", Reference Resolution 1280x720 kullanin.

01_screens/
  Ekranlarin tamami (arka plan dahil). Referans icin.
  00_main_menu, 01_game_screen_hud, 02_incident_file, 03_settings_dark, 04_settings_light,
  05_incoming_call (caliyor), 05b_call_listening (3 sn dinleme), 05c_call_ended_marker_on_map

02_ui_overlays_transparent/
  Ayni ekranlar, SADECE arayuz, arka plan seffaf. Tam ekran boyutunda oldugu icin
  Unity'de ekrana tam oturtunca her sey dogru yerine gelir.

03_ui_components/
  Tek tek kesilmis, seffaf arka planli parcalar (UNITS paneli, nufus paneli, olay isaretleri,
  olay dosyasi, ayarlar panelleri, cagri karti iki hali, menu haber bandi, menu alt cubugu, menu butonlari).

04_backgrounds/
  menu_background_loop_1280x720.mp4 : ana menu arka plani, 20 sn, kesintisiz dongu.
     Unity: Video Player -> Loop acik.
  menu_city_night_*.png : ayni sahnenin hareketsiz hali.

05_sprites/
  10x10 piksel simgeler. <isim>.png beyaz (rengi Unity'de verin), <isim>_preview.png onizleme.
  Import: Texture Type = Sprite, Filter Mode = Point, Compression = None, Pixels Per Unit = 10.

06_fonts/
  DispatchPixelTR.ttf  : basliklar, etiketler, sayilar, butonlar (sadece 8'in katlari: 16/24/32/48px).
                         Dogica Pixel'den turetildi, OFL lisansli - OFL.txt oyunla birlikte dagitilmali.
  IBMPlexSansCondensed : okunacak metinler (isimler, adresler, notlar). OFL lisansli.

Not: Haber bandi kayan yazi, yangin titremesi, moloz dususu gibi hareketler PNG'de durgun gorunur;
bunlarin hareketli hali tasarim linkinde ve menu arka plan videosunda.
