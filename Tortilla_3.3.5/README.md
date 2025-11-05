
#  Tortilla Baking Quality Control — Line‑Scan (HALCON + Delta PLC)

Endüstriyel tortilla/lavaş hatlarında **line‑scan kamera** ile alınan görüntülerin gerçek zamanlı
birleştirilmesi, **HALCON derin öğrenme classification** ile **pişme kalitesi tespiti** ve
**Delta PLC (Modbus/TCP)** üzerinden **OK/NOK** kontrolünün yapıldığı bir starter projedir.
Ayrıca ürün **çap/alan ölçümü** de yapar.

> Bu repo, prod hazır bir iskelet sunar: C# WinForms arayüz, HALCON entegrasyon sınıfları,
> line-scan için encoder/trigger parametre şablonları, PLC mapping dokümanı ve konfig dosyaları.

---

## ⚙️ Temel Bileşenler
- **Line‑Scan Toplama:** Encoder tabanlı satır senkronu; `GenICamTL/GigEVision2` parametre şablonları.
- **Sınıflandırma:** HALCON DL (Classification) — `overbaked`, `medium`, `underbaked`, `unbaked`.
- **Ölçüm:** Çap ve alan (mm/px kalibrasyonlu).
- **PLC:** Delta PLC Modbus/TCP — NOK, skor ve çap yazımı.
- **UI:** C# WinForms — 4 durum butonu, canlı durum göstergesi.

---

##  Hat Akışı (Line‑Scan)
1. Encoder/trigger ile satırlar toplanır.
2. Satırlar **şerit birleşimi** ile tek bir ürün görüntüsüne çevrilir.
3. HALCON DL modeli ile pişme sınıfı bulunur.
4. Çap/alan ölçülür ve tolerans kontrol edilir.
5. OK/NOK kararı verilir ve Delta PLC'ye yazılır.
6. UI'da durum butonları renk değiştirir.

