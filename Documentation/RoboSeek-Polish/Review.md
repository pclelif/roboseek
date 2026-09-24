# ROBOSEEK — görsel ve oynanış düzenlemesi

- Lobi logosu 1080p referansta 80 px aşağı alındı, 76 → 88 punto büyütüldü. Slogan yaklaştırıldı; robot ölçeği 0,87 oldu.
- 12 oyuncağın birer kopyası robotun iki yanına yerleştirildi. Sağdaki ayı ve panda en üst sırada; soldaki üst sırada tavşan solda. Raf grupları kameraya dönük.
- Hedef bulundu bildiriminin başlığı sarı.
- Süre değerleri (iki nokta dahil), toplanan oyuncak tikleri ve sonuç ekranındaki tik/çarpılar sarı.
- Bayılma bildirimi CombatHealth'in gerçek geri sayımını gösterir; duraklatmada süre durur, toparlanınca bildirim kapanır. Etkileşim ipuçları bayılma sırasında gizlenir.
- Sonuç ekranları aynı kaydedilmiş Jump_Air pozunu kullanır. Robot büyütüldü, logo ve slogan eklendi.
- İngilizce metinlerde eski Kenney fontu, Türkçe metinlerde Inter SemiBold kullanılır. Sayısal sayaçlar ve adet göstergelerinde okunaklı font korunur. Dil değişiminde font da güncellenir.
- Arka planın renk geçişleri korunarak doygunluğu ve parlaklığı azaltıldı; menü paneli 45 px sağa kaydırıldı.
- Lobi ve oyun arayüzünün güncel yerleşimi sahne dosyalarına kaydedildi.
- Deniz alanının dışına dört duvar ve aşağıya taban eklendi; sınır dışı konumlar için ek güvenlik kontrolü var. Su altında mavi arka plan ve sis kullanılır.
- Sahnedeki 14 eksik statik mesh collider tamamlandı. Üretilen nesnelerde eksik solid collider eklenir. NPC kapsülleri ve oyuncuyla örtüşme düzeltmesi eklendi.

## Skor — karar verilmedi, formül değiştirilmedi

Aktif solo akışı RoundGameLoop kullanıyor: bulunan oyuncak başına 500; tümü bulunduğunda 1500 tamamlama bonusu ve kalan saniye başına 10 puan. Örneğin 3 oyuncak, 2 dakika kalan süre: 1500 + 1500 + 1200 = 4200.

Projede ayrıca ScoreManager var: oyuncak başına 100, ilk bulana 25, sayılan vuruşa 10. ResultScreenController bu bileşen mevcutsa onu tercih ediyor. Mevcut solo sahne testinde bileşen devrede değil. Tek bir skor kaynağına geçiş sonraki tasarım kararı olmalı.

Skorlar tur/maç karşılaştırması için var; mevcut solo akışında kalıcı kişisel rekor, ödül veya satın alma sistemi bağlı değil. Öneri: solo için oyuncak + tamamlama + hız puanı ve kalıcı kişisel rekor; kozmetik açma daha sonra ayrı bir ilerleme sistemi olarak ele alınabilir. İlk bulan/vuruş puanları çok oyunculu tasarımda ayrıca değerlendirilmelidir.

## Doğrulama

Unity 6000.3.9f1 batch Play testi: lobi → oyun → gerçek oyuncak toplama → bayılma/duraklatma/toparlanma → sınır dışı düşüş koruması → sonuç ekranları → gerçek üç oyuncakla tur tamamlama → lobiye dönüş.

Otomatik kontroller tüm haritadaki her nesne çiftinin fiziksel çarpışmasını kapsamaz. Sınır koruması, NPC yaklaşması ve eksik collider düzeltmeleri kontrol edildi.

- [Lobi](lobby.png)
- [Toplanan oyuncak](collected.png)
- [Bayılma](knockout.png)
- [Round over](round-over.png)
- [Round complete](round-complete.png)
