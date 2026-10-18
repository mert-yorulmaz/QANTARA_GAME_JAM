DISPATCH - PHASE 1 (INSPECTION DESK) UI ASSET PAKETI
====================================================

Phase 1 kurumun bilgisayarinda (BIA OS) gecer. Tum gorseller 1280x720 tasarim
cozunurlugune gore 2 kat buyuklukte (2560x1440) disa aktarildi.
Unity: Canvas Scaler = "Scale With Screen Size", Reference Resolution 1280x720.

01_screens/   (YAZILI - sadece REFERANS)
  01_inspection_desk      : masaustu + dosya
  01b_file_approved       : onay damgasi basilmis dosya
  01c_rejection_slip      : ret formu acik, iki kural secili
  01d_file_rejected       : ret damgasi + gerekce
  02_rule_book            : kural kitabi acik (sayfa 3)
  Yazilarin yeri, fontu, boyutu ve rengi icin buraya bakin.

02_ui_overlays_transparent/   (YAZISIZ, arka plan seffaf, tam ekran)
  Ekrana tam oturtunca her parca dogru yerine gelir.
  Kural kitabi acikken arkadaki karartma bu dosyada YOK: Unity'de tam ekran
  bir Image ile verin -> renk #080C12, opaklik %62.

03_ui_components/   (YAZISIZ, tek tek parcalar)
  hud/
    case_errors_panel_ok / _error : sol ust CASE/ERRORS paneli. Hata 0 iken "ok"
                                    (yesil simge), hata varsa "error" (kirmizi simge).
    rule_book_button              : sag ust kural kitabi butonu
    taskbar                       : alttaki BIA OS gorev cubugu
  inspection_file/
    inspection_file_panel         : dosya, FOTOGRAF BOS (her dosyada farkli bina
                                    fotografi kodla konacak)
    xray_overlay_grid             : X-RAY acikken fotografin ustune binen plan izgarasi.
                                    Catlak varsa kirmizi (#FF5E6C) cizgi olarak ustune cizin.
    btn_xray_off / btn_xray_on    : X-RAY butonunun iki hali
    btn_reject / btn_approve / btn_next_file
    stamp_approved / stamp_rejected : bos damga cerceveleri (seffaf). Yazi:
                                    APPROVED / REJECTED + altinda "RULE 2.2" gibi gerekce.
  rejection_slip/
    rejection_slip_panel          : ret formu; kural kutulari ve onay butonu YOK
    chip_normal / chip_selected   : 8 kural kutusu (4x2 izgara) - secilmemis/secili
    btn_confirm_disabled / _enabled : en az bir kural secilince "enabled"
    btn_cancel, link_check_rule_book
  rule_book/
    rule_book_panel               : bos kitapcik (sayfa icerigi ve sekmeler yok)
    tab_1..5_active / _inactive   : sagdaki bolum sekmeleri
    rule_badge_navy / _red        : kural numarasi rozeti (yapisal kurallar kirmizi)
    verdict_tag_reject            : kuralin altindaki REJECT etiketi cercevesi
    btn_prev / btn_next / btn_close

04_backgrounds/
  workstation_background : BIA OS masaustu (izgara, tarama cizgileri, kurum filigrani)

05_sprites/
  10x10 piksel simgeler. <isim>.png beyaz (rengi Unity'de verin), _preview.png onizleme.
  Import: Texture Type = Sprite, Filter Mode = Point, Compression = None, Pixels Per Unit = 10.

06_fonts/
  DispatchPixelTR  : basliklar, etiketler, sayilar, butonlar (16/24/32/48px).
                     OFL lisansli, OFL.txt oyunla birlikte dagitilmali.
  IBMPlexSansCondensed : okunacak metinler (adres, notlar, kurallar). OFL lisansli.

Renkler (Phase 2 ile ayni palet):
  Kagit #ECDCB0 · sekme #D9C08A · kenar #B89B5E · murekkep #2A2216 · etiket #6B5A36
  Onay / ana buton #1E3A5F · Ret / damga #A8241C · HUD zemin #3A3A3A · HUD cerceve #141413
