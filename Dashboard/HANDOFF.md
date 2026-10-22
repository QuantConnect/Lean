# Handoff: LEAN Dashboard → ไปต่อบน Windows

อัปเดตล่าสุด: 2026-10-22 (ทำบน macOS, Apple Silicon)

## เป้าหมาย
ใช้ QuantConnect LEAN (repo นี้) แบบ local ไม่ต้องสมัคร QuantConnect โดยมี:
1. Dashboard บนเว็บ: สั่งรัน backtest และดูผลแบบ real-time ✅ เสร็จแล้ว
2. ข้อมูลทอง (XAUUSD) ล่าสุด 3 เดือน ⏳ กำลังดึงอยู่ ดูหัวข้อสถานะ
3. ราคาทองสดจาก **XM ผ่าน MT5** ⬜ เป็นเหตุผลที่ย้ายมา Windows เพราะ Python package `MetaTrader5` ใช้ได้แค่บน Windows
4. เขียนกลยุทธ์ทองแล้วทดสอบกับข้อมูลล่าสุด ⬜

## สิ่งที่ทำไปแล้ว

### ไฟล์ใหม่ใน `Dashboard/` (ยังไม่ได้ commit)
| ไฟล์ | หน้าที่ |
|---|---|
| `server.py` | FastAPI: สั่งรัน LEAN ทีละครั้งด้วย config แยก, รับ packet จาก LEAN ผ่าน ZeroMQ แล้วส่งต่อให้เบราว์เซอร์ทาง WebSocket, เก็บประวัติการรันใน `results/<id>/` |
| `index.html` | หน้าเว็บ: ฟอร์มควบคุม (กลยุทธ์, วันที่, เงินทุน, parameters), ประวัติการรัน, กราฟ equity/benchmark/drawdown แบบ live, ตาราง trades/orders, log และสถิติ |
| `Dockerfile`, `Dockerfile.dockerignore`, `docker-compose.yml` | Build LEAN แล้วรัน server ใน container, mount `../Data` แบบอ่านอย่างเดียว และ `./results` |
| `requirements.txt` | fastapi 0.115.12, uvicorn[standard] 0.34.2, pyzmq 27.2.0 |
| `fetch_dukascopy.py` | ดึงแท่งราคารายนาทีของ XAUUSD จาก Dukascopy แล้วเขียนเป็นรูปแบบ `Data/cfd/oanda` ของ LEAN (รันซ้ำแล้วทำต่อจากเดิมได้) |
| `.gitignore` | ไม่เอา `results/` เข้า git |

### การแก้โค้ด LEAN
- `Queues/JobQueue.cs`: เพิ่ม config `period-start`, `period-finish` (YYYYMMDD), `cash-amount` และ `cash-currency` (default USD) เพื่อ override วันที่และเงินทุนของกลยุทธ์ server ใช้ค่าพวกนี้

### ไฟล์ของผู้ใช้ (ไม่ได้แตะ)
- `Algorithm.CSharp/GoldMovingAverageCrossAlgorithm.cs` เป็นกลยุทธ์ SMA 50/200 บนทอง
- `Launcher/config.json` ถูกแก้ให้ `algorithm-type-name` เป็น GoldMovingAverageCrossAlgorithm

## สถานะข้อมูลทอง ⚠️
- `fetch_dukascopy.py --days 90` กำลังรันอยู่บน Mac ตอนเขียนเอกสารนี้ ได้ข้อมูลถึงประมาณ 2026-09-15 แล้ว
- **ไฟล์รายนาทีใหม่ไม่ได้อยู่ใน git** (`.gitignore` ตัด `*Data/*` ออก) ส่วน `hour/xauusd.zip` และ `daily/xauusd.zip` อยู่ใน git และจะถูกเขียนใหม่ตอนดึงครบเท่านั้น
- **บน Windows ให้ดึงใหม่:** `python Dashboard\fetch_dukascopy.py --days 90`
- Dukascopy จำกัดจำนวน request: ต้องส่ง User-Agent ไม่งั้นได้ 429 และถ้าดึงถี่จะได้ 503 สคริปต์ลองใหม่ให้เองแล้ว (backoff, หน่วง 3 วินาทีต่อวัน)
- รูปแบบข้อมูล:
  - URL ของ Dukascopy นับเดือนจาก 0
  - ไฟล์ `.bi5` เป็น LZMA เก็บ record ขนาด 24 byte (`>IIIIIf` = วินาที, open, close, low, high, volume)
  - ราคาเป็นจำนวนเต็มต้องหารด้วย 1000
  - ข้อมูลทองใน LEAN ใช้เวลา UTC

## วิธีรัน
```powershell
# Dashboard (ต้องมี Docker Desktop)
cd Dashboard
docker compose up -d --build      # เปิด http://127.0.0.1:8000
docker compose down

# รัน LEAN ตรงๆ (ต้องมี .NET 10 SDK)
dotnet build QuantConnect.Lean.sln
cd Launcher\bin\Debug
dotnet QuantConnect.Lean.Launcher.dll
```
ถ้าจะรัน server โดยไม่ใช้ Docker ให้ตั้ง env ก่อน: `LEAN_DIR`, `LEAN_DATA_DIR`, `RESULTS_DIR` และ `ALGORITHM_SOURCE_DIR=<repo>\Algorithm.CSharp`

## จุดที่ต้องระวัง
- **Locale:** ต้องรัน LEAN ด้วย `en_US.UTF-8` ถ้าเครื่องตั้งภาษาไทย ปฏิทินจะเป็น พ.ศ. และวันที่ในผลลัพธ์จะเพี้ยน ส่วน `C.UTF-8` ทำให้หาคลาสกลยุทธ์ไม่เจอ เพราะ `EndsWith` ใน `Common/Extensions.cs:2484` เทียบตาม culture server ตั้งค่านี้ให้แล้ว แต่บน Windows ควรเช็กว่า culture ของระบบไม่ทำให้เกิดปัญหาเดียวกัน
- **Messaging:** `StreamingMessageHandler` เป็น NetMQ PUSH ที่ bind พอร์ต `desktop-http-port` ถ้ามีตัวรับ (PULL) หลายตัว ข้อมูลจะถูกแบ่งสลับกันไป ต้องมีตัวรับตัวเดียว
- **Packet:** key เป็น PascalCase ส่วนไฟล์ผลลัพธ์เป็น camelCase ฝั่งเว็บใช้ `pick()` อ่านได้ทั้งสองแบบ ระหว่างรันข้อมูลกราฟ, orders และ trades มาเป็น delta และ packet ตอนจบ (Progress = 1) มีข้อมูลเต็ม
- **CLI:** launcher ไม่รับ option ทาง command line ส่วนใหญ่ (เช่น `--desktop-http-port`) ต้องสร้าง config แล้วส่ง `--config <path>` แทน
- **ข้อจำกัด:** ตอนนี้รันได้เฉพาะกลยุทธ์ C# ยังไม่รองรับ Python ถ้าเพิ่มหรือแก้กลยุทธ์ต้อง `docker compose up -d --build` ใหม่
- **ยังไม่ได้ทดสอบ:** ปุ่มหยุด และการแสดงผลในเบราว์เซอร์จริง (ทดสอบแค่ API, WebSocket และ logic การต่อ delta)

## ขั้นต่อไปบน Windows
1. **ราคาสดจาก XM:**
   - ติดตั้ง MT5 ของ XM แล้วล็อกอิน **บัญชี demo** และให้ symbol `GOLD` แสดงใน Market Watch
   - `pip install MetaTrader5` แล้วใช้ `mt5.initialize()`, `mt5.symbol_info_tick("GOLD")` และ `mt5.copy_rates_range(...)` สำหรับข้อมูลย้อนหลัง
   - เพิ่ม endpoint หรือ WebSocket `/ws/live/GOLD` ใน server.py และกราฟราคาสดใน index.html
   - MT5 ต้องรันบน Windows host ไม่ใช่ใน Docker Linux ดังนั้นตัวดึงราคาจาก MT5 ต้องรันนอก container แล้วส่งข้อมูลเข้า server
2. **ข้อมูลย้อนหลังของ XM:** ใช้ `copy_rates_range` (M1) แล้วแปลงเป็นรูปแบบ LEAN เหมือน fetch_dukascopy.py เพื่อให้ backtest ใช้ราคาเดียวกับที่จะเทรดจริง
3. **กลยุทธ์ทอง:** เขียนแล้ว backtest กับข้อมูล 3 เดือนล่าสุด โดยแยกช่วงข้อมูลไว้ทดสอบ (out-of-sample) และระวัง overfit
4. **ทีหลัง:** paper trading หรือส่งคำสั่งเข้า XM (ต้องเขียน brokerage เอง และควรใช้กับบัญชี demo เท่านั้น)
