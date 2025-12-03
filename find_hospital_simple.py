"""
Tìm bệnh viện có nhiều người đánh giá nhất ở quận Phú Nhuận, HCM
Chỉ sử dụng Text Search API (đơn giản)
"""

import requests
import time

# API key của bạn
API_KEY = "AIzaSyCbddLqZ2gzy97KvcItAHbaofjQeNVT8XE"


def search_hospitals(api_key: str):
    """
    Tìm bệnh viện ở Phú Nhuận bằng Text Search API
    """
    url = "https://maps.googleapis.com/maps/api/place/textsearch/json"
    
    # Query tìm kiếm
    query = "bệnh viện Phú Nhuận Ho Chi Minh City"
    
    print(f"🔍 Đang tìm: '{query}'...")
    
    params = {
        "query": query,
        "key": api_key,
        "type": "hospital",
        "language": "vi"
    }
    
    all_results = []
    seen_ids = set()
    
    # Lấy tất cả các trang kết quả
    next_page_token = None
    page = 1
    
    while True:
        if next_page_token:
            params["pagetoken"] = next_page_token
            time.sleep(2)  # Đợi trước khi lấy trang tiếp theo
            print(f"   📄 Đang lấy trang {page}...")
        
        response = requests.get(url, params=params)
        data = response.json()
        
        if data["status"] != "OK":
            if data["status"] != "ZERO_RESULTS":
                print(f"   ⚠️  Lỗi: {data.get('status')}")
            break
        
        results = data.get("results", [])
        print(f"   ✅ Trang {page}: Tìm thấy {len(results)} kết quả")
        
        # Lọc chỉ lấy bệnh viện ở Phú Nhuận
        for place in results:
            place_id = place.get("place_id")
            if place_id and place_id not in seen_ids:
                address = place.get("formatted_address", "").lower()
                
                # Chỉ lấy nếu địa chỉ có "Phú Nhuận"
                if "phú nhuận" in address or "phu nhuan" in address:
                    all_results.append(place)
                    seen_ids.add(place_id)
        
        # Kiểm tra có trang tiếp theo không
        next_page_token = data.get("next_page_token")
        if not next_page_token:
            break
        
        page += 1
    
    return all_results, page


def main():
    """Hàm chính"""
    print("=" * 60)
    print("🏥 TÌM BỆNH VIỆN Ở PHÚ NHUẬN")
    print("=" * 60)
    print()
    
    # Tìm bệnh viện
    hospitals, num_pages = search_hospitals(API_KEY)
    
    if not hospitals:
        print("\n⚠️  Không tìm thấy bệnh viện nào!")
        return
    
    print(f"\n📊 Tìm thấy {len(hospitals)} bệnh viện ở Phú Nhuận\n")
    
    # Sắp xếp theo số đánh giá
    hospitals_sorted = sorted(
        hospitals,
        key=lambda x: x.get("user_ratings_total", 0),
        reverse=True
    )
    
    # Bệnh viện có nhiều đánh giá nhất
    top = hospitals_sorted[0]
    
    print("=" * 60)
    print("🏆 BỆNH VIỆN CÓ NHIỀU ĐÁNH GIÁ NHẤT")
    print("=" * 60)
    print(f"📛 Tên: {top.get('name', 'N/A')}")
    print(f"⭐ Rating: {top.get('rating', 'N/A')}/5.0")
    print(f"👥 Số đánh giá: {top.get('user_ratings_total', 0):,}")
    print(f"📍 Địa chỉ: {top.get('formatted_address', 'N/A')}")
    print()
    
    # Top 5 bệnh viện
    print("=" * 60)
    print(f"📋 TOP {min(5, len(hospitals_sorted))} BỆNH VIỆN")
    print("=" * 60)
    
    for i, h in enumerate(hospitals_sorted[:5], 1):
        print(f"\n{i}. {h.get('name', 'N/A')}")
        print(f"   ⭐ {h.get('rating', 'N/A')}/5.0 | 👥 {h.get('user_ratings_total', 0):,} đánh giá")
        print(f"   📍 {h.get('formatted_address', 'N/A')}")
    
    print(f"\n💰 Chi phí: ~{num_pages} requests")


if __name__ == "__main__":
    main()

