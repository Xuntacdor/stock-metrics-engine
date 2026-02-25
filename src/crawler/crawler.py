import os
import requests
import pyodbc
import time
from datetime import datetime
from dotenv import load_dotenv

load_dotenv()

DB_SERVER = os.getenv("DB_SERVER")
DB_NAME = os.getenv("DB_NAME")
DB_USER = os.getenv("DB_USER")
DB_PASSWORD = os.getenv("DB_PASSWORD")

DNSE_API_URL = os.getenv("DNSE_API_KEY")

def get_db_connection():
    conn_str = (
        f"DRIVER={{ODBC Driver 18 for SQL Server}};"
        f"SERVER={DB_SERVER},1433;"
        f"DATABASE={DB_NAME};"
        f"UID={DB_USER};"
        f"PWD={DB_PASSWORD};"
        "TrustServerCertificate=yes;" 
    )
    return pyodbc.connect(conn_str)

def sync_symbol(cursor, symbol):
    cursor.execute("SELECT 1 FROM Symbols WHERE Symbol = ?", symbol)
    if not cursor.fetchone():
        print(f"Adding new symbol: {symbol}")
        cursor.execute(
            "INSERT INTO Symbols (Symbol, CompanyName, Exchange) VALUES (?, ?, ?)",
            symbol, f"Company {symbol}", "HOSE"
        )

def fetch_and_save(symbol):
    print(f"Fetching data for {symbol}...")
    
    end_time = int(time.time())
    start_time = end_time - (365 * 24 * 60 * 60) 
    
    params = {
        "symbol": symbol,
        "resolution": "1D",
        "from": start_time,
        "to": end_time
    }
    
    try:
        response = requests.get(DNSE_API_URL, params=params, timeout=10)
        data = response.json()
        
        if "t" not in data or not data["t"]:
            print(f"No data available for {symbol}")
            return

        candles = []
        for i in range(len(data['t'])):
            row = (
                symbol,
                data['t'][i],    
                data['o'][i],     
                data['h'][i],     
                data['l'][i],      
                data['c'][i],      
                data['v'][i]       
            )
            candles.append(row)

        conn = get_db_connection()
        cursor = conn.cursor()
        
        sync_symbol(cursor, symbol)
        
        cursor.execute(
            "DELETE FROM Candles WHERE Symbol = ? AND Timestamp >= ? AND Timestamp <= ?",
            symbol, start_time, end_time
        )
        
        cursor.executemany(
            """
            INSERT INTO Candles (Symbol, Timestamp, [Open], [High], [Low], [Close], Volume)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            candles
        )
        
        conn.commit()
        print(f"Successfully saved {len(candles)} candles for {symbol}")
        conn.close()

    except Exception as e:
        print(f"Error occurred: {e}")

if __name__ == "__main__":
    watchlist = ["FPT", "VIC", "MWG", "ACB", "TCB"]
    
    print("--- Starting Stock Metrics Data Engine ---")
    for ticker in watchlist:
        fetch_and_save(ticker)
        time.sleep(1)
    print("--- Data Sync Completed ---")