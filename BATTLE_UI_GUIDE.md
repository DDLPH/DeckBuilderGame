# Battle UI — THE HOLLOW BEYOND

โปรเจกต์ที่แก้: `C:/Users/iiTzDemmy/Desktop/2D-Roguelike-Deck-Building`

## ทดลองเล่น

1. เปิด `Assets/Scenes/MainMenu.unity` แล้วกด Play
2. กด New Run หรือ Continue แล้วเลือกด่านบน Map
3. ลากการ์ดโจมตีไปปล่อยบนศัตรู ลากการ์ดป้องกัน/จั่ว/พลังงานไปปล่อยบนผู้เล่น
4. หรือคลิกการ์ดหนึ่งครั้ง แล้วคลิกเป้าหมาย
5. คลิกขวาหรือ Esc เพื่อยกเลิก ปล่อยผิดที่/พลังงานไม่พอไม่เสียทรัพยากร
6. End Turn หรือ E จบเทิร์น ปุ่มกองจั่ว/กองทิ้ง/กองเผาเปิดดูรายการได้
7. Pause หรือ Esc เมื่อไม่ได้เลือกการ์ดเพื่อพักเกม; การกลับเมนูมีคำเตือนว่าไม่เซฟกลาง Battle

เปิด BattleScene โดยตรงโดยไม่มีรอบเล่นจะแจ้งให้กลับไปเริ่มจาก Main Menu แทนการสร้างรอบทดสอบทับเซฟจริง

ภาษาใช้ค่าที่เลือกจาก Main Menu ไม่มีปุ่มเปลี่ยนภาษาซ้ำใน Battle เสียงใช้ระดับเสียงกลางเดียวกับเมนู

## ชุดการ์ดเริ่มต้นและรางวัล

เด็คเริ่มต้นยังเป็น Strike 5 ใบ + Defend 5 ใบ, HP 80, พลังงาน 3, จั่วเริ่มเทิร์นให้มี 5 ใบ

| การ์ด | พลังงาน | เอฟเฟกต์ |
|---|---:|---|
| Strike / ฟันโจมตี | 1 | โจมตีเดี่ยว 6 |
| Defend / ตั้งรับ | 1 | เกราะ 5 |
| Astral Cut / คมดาบดารา | 1 | โจมตีเดี่ยว 9 |
| Moon Ward / ม่านจันทรา | 1 | เกราะ 8 |
| Starfall / ดาวถล่ม | 2 | โจมตีศัตรูทุกตัว 7 |
| Eclipse / คมคราส | 2 | โจมตีเดี่ยว 16 |
| Insight / หยั่งรู้ | 1 | จั่วเพิ่ม 2 ใบ |
| Focus / รวมสมาธิ | 0 | พลังงาน +1 แล้วเข้ากองเผา ใช้ซ้ำไม่ได้ใน Battle เดียวกัน |

ยังมี Strike+ (9) และ Defend+ (เกราะ 8) สำหรับระบบอัปเกรดเดิม รวม 10 CardData assets

การ์ดใหม่ 6 แบบและการ์ดอัปเกรด 2 แบบอยู่ใน pool รางวัลและร้านค้าแล้ว; รายการกู้เซฟรองรับครบ 10 assets

## สมดุลเริ่มต้น (ต้องทดลองเล่นปรับต่อ)

- มอนธรรมดา HP 22 จำนวน 1–2 ตัว: โจมตี 5 → เกราะ 3 → โจมตี 6
- Elite HP 52 จำนวน 1 ตัว: โจมตี 8 → เกราะ 5 → โจมตี 10
- Boss HP 100 จำนวน 1 ตัว: โจมตี 9 → เกราะ 7 → โจมตี 12 → โจมตี 6
- วน pattern เดิมและแสดง intent ก่อนทำจริง
- รางวัลชนะทั่วไป 25 ทอง; ราคาการ์ดร้านค้า 45 ทอง
- Rest และ Event ยังคงค่าฮีลเดิม ไม่เพิ่มระบบใหม่ที่ไม่เกี่ยวกับ Battle

เป้าหมายคือด่านทั่วไปประมาณ 3–5 เทิร์น ส่วน Elite/Boss นานกว่า การกระจายไพ่และการเลือกเล่นส่งผลต่อระยะเวลาจริง ตัวเลขนี้ไม่ใช่ข้อยืนยันว่าทุกผู้เล่นจะรู้สึกสมดุล ต้อง playtest ทั้งรอบอีกครั้ง

## โครงสร้าง

- BattleManager: ผู้ตัดสินเทิร์น เป้าหมาย พลังงาน และ resolve การ์ดเพียงครั้งเดียว
- DeckManager: กองจั่ว มือ กองทิ้ง กองเผา และการจั่วเพิ่ม
- CosmicBattleUI: HUD, hand, pause/pile overlays, input presentation และเสียง
- CosmicBattleCard: pointer drag/click, hover, return motion
- CosmicBattleActor: sprite, health/block/intent, hit feedback, breathing/lunge
- BattleUIFactory: สร้างองค์ประกอบ UI ด้วย palette/frame ของ CosmicUITheme
- DrawCardsEffect / GainEnergyEffect: รับข้อมูลจาก CardEffectContext ไม่เก็บ run state เอง

UI เดิมถูกเก็บใน `Canvas/LegacyBattleUI` (alpha 0, ไม่รับ input) เพื่อรักษา spawn/data references ไม่ได้ลบต้นฉบับทั้งหมด

นำ EnemyData component ที่ซ้ำและไม่มี action pattern ออกจาก child ของ NormalEnemy/Elite/Boss เท่านั้น ไม่ลบ child GameObject หรือภาพ เก็บ EnemyData หลักที่ระบบ Spawn ใช้ไว้ การเปลี่ยน prefab นี้ย้อนคืนได้จาก Git รุ่นก่อนหน้า

## ขอบเขตเซฟ

ระบบเดิมเซฟที่จุดปลอดภัย เมื่อออกกลาง Battle การเล่นต่อจะเริ่มจาก checkpoint ก่อนเข้าด่าน ไม่ใช่เซฟกลางเทิร์น

ยังไม่ Commit หรือ Push งานชุดนี้ หลังทดลองผ่านให้ทำ GitHub Savepoint ทั้ง origin และ backup เมื่อผู้ใช้สั่ง

## ภาพ

สร้างด้วย built-in imagegen ไม่ใช่ CLI; PNG ตัวละครมี alpha จริง ส่วนฉากเป็นภาพนิ่ง ความเคลื่อนไหวเกิดจากโค้ด Unity ไม่ใช่ MP4

ไฟล์ที่ใช้งานจริงอยู่ใน `Assets/UI/Cosmic/Art/`:

- BattleWanderer.png — extract/refine protagonist: right-facing human traveler, indigo cloak, ivory scarf, cards and short sword, whole body, transparent alpha, no environment, simplified hand-painted texture.
- BattlePilgrim.png — extract masked pilgrim: left-facing, cracked ivory mask and torn cloak, remove crescent halo/environment, compact full-body transparent sprite.
- BattleSovereign.png — extract Eclipse Sovereign: four arms, ivory mask and attached eclipse halo, simplify ornaments, whole body transparent, no terrain/fog.
- BattleLandscape.png — 16:9 static ruined stone terrace, dark navy/purple cosmic ruins, open combat areas, no characters/text/UI.

สรุป prompt เหล่านี้บันทึกเจตนาและข้อจำกัดของภาพ ชื่อไฟล์ใหม่ไม่ทับไฟล์คอนเซปต์ที่เคยสร้าง

## ตรวจสอบ

Editor checks: `CosmicBattleChecks.Run` ตรวจทั้ง 3 ประเภทด่าน, validation, energy, pile counts, Focus Exhaust, Insight draw, Starfall all-target, drag rejection, raycast และ preview TH/EN โดยไม่เขียนเซฟผู้เล่น

Opt-in Play smoke: `CosmicBattleChecks.RunPlay` พร้อม command-line flag `-cosmic-battle-smoke` ทดสอบ pointer handlers และ coroutine จริงด้วย RunData ในหน่วยความจำ ตรวจ hash ของเซฟจริงว่าไม่เปลี่ยน ไม่ใช้ fixture นี้เวลาเล่นตามปกติหรือใน build

ชุดทดสอบล่าสุดแยกเซฟไปยังโฟลเดอร์ชั่วคราวเฉพาะทดสอบ (มีเฉพาะใน Unity Editor ไม่มีใน build) จึงทดสอบ Reward → Map → บันทึก → Main Menu → Continue รวมถึงชนะ Boss และแพ้ได้ โดยตรวจ hash ว่าเซฟผู้เล่นจริงไม่เปลี่ยน

ยังไม่ได้ทดสอบความสนุกตลอดทั้ง run กับผู้เล่นจริง หรือ Build Windows release ในงานนี้

## เก็บบั๊กจบรอบ / เสียง / Loading

- ชนะ Boss: หน้าต่าง RUN COMPLETE แสดงชั้น ทอง จำนวนการ์ด และกลับ Main Menu ได้
- แพ้: หน้าต่าง RUN ENDED พร้อมกลับ Main Menu; Continue ถูกปิดสำหรับรอบที่จบแล้ว
- หน้าสรุปผลยึด Canvas ของ Battle และ modal ย้ายขึ้น root Canvas เสมอ ไม่ซ่อนใต้ EnemyUI/LegacyBattleUI อีก
- ไม่รับการ์ด/เทิร์น/การเลือกด่านซ้ำขณะเปลี่ยนฉาก; Loading ใช้ LoadSceneAsync และเวลา unscaled รองรับการออกจาก Pause
- หน้า Loading เป็น overlay โทนเดียวกับเกม ไม่จำเป็นต้องเพิ่ม scene หรือแก้ Build Profiles
- เพลงเดิมของผู้ใช้ยังอยู่ครบ; ด่านบอสใช้ EclipseSiege.wav เป็นค่าเริ่มต้น สามารถเปลี่ยน Boss Music Clip ที่ Canvas → Cosmic Battle UI ได้
- เพลงบอสเป็นดนตรีสังเคราะห์ต้นฉบับ ลูป 48 วินาที โทน D minor มีเบส จังหวะกลอง และเสียงประสาน ไม่มีเสียงร้อง ไม่ได้ใช้ไฟล์เพลงบุคคลอื่น
- เสียงใหม่ 9 แบบ: Hover, Click, Card, Attack, Guard, Reject, Turn, Victory, Defeat อยู่ใน Assets/UI/Cosmic/Resources/CosmicAudio/
- เสียงทั้งหมดผ่านระดับ Volume เดียวกับ Settings; ไม่สร้าง AudioListener เพิ่ม
- ปุ่ม Main Menu ปิดเฉพาะ callback AudioSource.Play เก่าขณะ runtime และใช้เจ้าของเสียงคลิกเพียงตัวเดียว ไม่ลบ callback ระบบเกม
- ปุ่ม Map และปุ่มเดิมใน Reward/Rest/Shop ใช้ชุดเสียงเดียวกันโดยไม่เปลี่ยน layout หน้าที่ยังไม่ได้ออกแบบใหม่
- ปรับ API ค้นหา Manager/Object และ TextMeshPro word wrapping ที่ Unity 6000.5.0f1 แจ้งว่าเลิกใช้แล้ว โดยไม่เปลี่ยนกติกาเกม
- Tools → Cosmic UI → Bake Original Boss Music ใช้สร้างไฟล์เสียงใหม่จากสูตรในโค้ดอีกครั้งได้ แต่จะเขียนทับเฉพาะไฟล์เสียงที่สร้างไว้ในโฟลเดอร์ CosmicAudio จึงไม่ควรกดหากกำลังปรับไฟล์เหล่านี้เอง

ทดสอบ Editor: CosmicPolishChecks.Run (รวม CosmicBattleChecks.RunFinal)

ทดสอบ Play: CosmicBattleChecks.RunPlay พร้อม -cosmic-battle-smoke ตรวจทั้ง 3 encounter, การลาก/คลิก, pause, reward และ Continue, ชนะ Boss, แพ้, raycast ปุ่มผลลัพธ์, เปิด Battle ตรงโดยไม่มี run, เพลงบอส, เสียงคลิกครั้งเดียว และการเปลี่ยนฉากซ้ำ

เมื่อทดลองด้วยตัวเองผ่านแล้ว จุดนี้เป็น GitHub Savepoint ที่ควร Commit/Push ทั้ง origin และ backup ตามคำสั่งผู้ใช้ ไม่ทำอัตโนมัติ

บันทึกทดสอบอยู่ที่ `C:/Users/iiTzDemmy/Desktop/GameProject/unity-battle-delivery.log` และ `C:/Users/iiTzDemmy/Desktop/GameProject/unity-battle-play-checks-2.log`
