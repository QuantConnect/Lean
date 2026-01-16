import pandas as pd
from openai import OpenAI
from pydantic import BaseModel, Field
from typing import List
import json

class DateWithTime(BaseModel):
    date: str = Field(description="Dates must be M/D/YYYY (no leading zeros)")
    hour: str = Field(description="Times must be HH:MM:SS (24h) in U.S. Central Time")

class HolidayCalendarEntry(BaseModel):
    holidays: List[str] = Field(description="Full holidays, this is, when the market is CLOSED ALL DAY.")
    bankHolidays: List[str] = Field(description="Bank Holidays, this is, when the market is not CLOSED ALL DAY.")
    earlyCloses: List[DateWithTime] = Field(description="Early market closes, this is, the market does not open at night.")
    lateOpens: List[DateWithTime] = Field(description="Late market opens, this is, the market opens until night.")

class HolidayCalendar(BaseModel):
    interest: HolidayCalendarEntry = Field(description="Asset class 'Interest Rates'")
    energy: HolidayCalendarEntry = Field(description="Asset class 'Energy'")
    equity: HolidayCalendarEntry = Field(description="Asset class 'Equities'")
    grains: HolidayCalendarEntry = Field(description="Asset class 'Grains'")
    fx: HolidayCalendarEntry = Field(description="Asset class 'FX'")
    metals: HolidayCalendarEntry = Field(description="Asset class 'Metals'")
    livestock: HolidayCalendarEntry = Field(description="Asset class 'Livestock'")
    dairy: HolidayCalendarEntry = Field(description="Asset class 'Dairy'")
    crypto: HolidayCalendarEntry = Field(description="Asset class 'Cryptocurrences'")
    lumber: HolidayCalendarEntry = Field(description="Asset class 'Lumber'")

def extract_holiday_schedule(path):
    df = pd.read_excel(path, header=None)

    # Row 4 contains the dates
    date_row = 4
    dates = df.iloc[date_row, 1:]

    records = {}

    for row_idx in range(date_row + 1, len(df)):
        asset_class = df.iloc[row_idx, 0]
        if pd.isna(asset_class):
            continue
        
        for col_idx, date in dates.items():
            cell = df.iloc[row_idx, col_idx]

            asset_class_str = str(asset_class).strip()
            if asset_class_str not in records.keys():
                records[asset_class_str] = []
            
            records[asset_class_str].append({
                "date": pd.to_datetime(date).strftime("%m/%d/%Y"),
                "raw_text": str(cell).strip().replace("PREOPEN", "CLOSED") if not pd.isna(cell) else "CLOSED ALL DAY"
            })

    return records

def transform_holiday_schedule(records, regular_schedule):
    client = OpenAI()
    prompt = """
    For each date in each class from the provided source, classify the date as holiday, bank holiday,
    early close or late open.
    - If the date is a weekend do not add it to any category.
    - No date classified as a Holiday can be also a Bank Holiday.
    - Use the regular schedule provided to know if a given date is a late open or early close based on the regular
      open and close time of certain asset.
    - If a date is classified as a Bank Holiday, include it in the bank holidays list but also include it either as a late open or early close.
      Example: 
      {
        "date": "02/16/2026",
        "raw_text": "12:00 Trade Date: 2026-02-17 (CLOSED) \n17:00 Trade Date: 2026-02-17 (OPEN)"
      },

      This date is a bank holiday as the market is not CLOSED ALL DAY but has an early close at 12h
      and a late open at 17h.
    """
    response = client.responses.parse(
        model = "gpt-5.2",
        reasoning = {"effort": "high"},
        input = [
            {
                "role": "developer",
                "content": prompt
            },
            {
                "role": "user",
                "content": f"Sources: {records}"
            },
            {
                "role": "user",
                "content": f"Regular schedule: {regular_schedule}"
            }
        ],
        text_format=HolidayCalendar
    )

    with open("client_response.json", "w") as f:
        json.dump(response.output_parsed.model_dump(), f, indent = 2)
    return response.output_parsed

# Extract record for each CME group Holiday
new_year_records = extract_holiday_schedule("holiday_calendars/2026-NewYear.xlsx")
mlk_records = extract_holiday_schedule("holiday_calendars/2026-MLK.xlsx")
presidents_records = extract_holiday_schedule("holiday_calendars/2026-Presidents.xlsx")
good_friday_records = extract_holiday_schedule("holiday_calendars/2026-GoodFriday.xlsx")
memorial_records = extract_holiday_schedule("holiday_calendars/2026-Memorial.xlsx")
juneteenth_records = extract_holiday_schedule("holiday_calendars/2026-Juneteenth.xlsx")
independence_records = extract_holiday_schedule("holiday_calendars/2026-Independence.xlsx")
labor_records = extract_holiday_schedule("holiday_calendars/2026-Labor.xlsx")
thanksgiving_records = extract_holiday_schedule("holiday_calendars/2026-Thanksgiving.xlsx")
christmas_records = extract_holiday_schedule("holiday_calendars/2026-Christmas.xlsx")
new_year_2027_records = extract_holiday_schedule("holiday_calendars/2026-NewYear2027.xlsx")

calendars = [new_year_records, mlk_records, presidents_records, good_friday_records, memorial_records, juneteenth_records, independence_records, labor_records, thanksgiving_records, christmas_records, new_year_2027_records]
records_merged = {}

# Merge each holiday record and save it as a json
for k in new_year_records.keys():
    records_merged[k] = tuple(record[k] for record in calendars)

with open("records.json", "w") as f:
    json.dump(records_merged, f, indent = 4)

# Open json with regular trading hours per class
with open("regular_schedule.json", "r") as file:
    regular_schedule = json.load(file)

print(transform_holiday_schedule(records_merged, regular_schedule))