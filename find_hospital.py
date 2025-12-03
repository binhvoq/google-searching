"""
Tìm bệnh viện có nhiều người đánh giá nhất ở quận Phú Nhuận, HCM, Vietnam
Sử dụng Geocoding API + Places Nearby Search (theo địa lý chính thống)
"""

import requests
import time
import math

# API key của bạn
API_KEY = "AIzaSyCbddLqZ2gzy97KvcItAHbaofjQeNVT8XE"


def calculate_distance(lat1, lon1, lat2, lon2):
    """
    Tính khoảng cách giữa 2 điểm (Haversine formula)
    Trả về khoảng cách tính bằng mét
    """
    R = 6371000  # Bán kính Trái Đất (mét)
    
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    delta_phi = math.radians(lat2 - lat1)
    delta_lambda = math.radians(lon2 - lon1)
    
    a = math.sin(delta_phi/2)**2 + math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda/2)**2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1-a))
    
    return R * c


def get_phu_nhuan_location(api_key: str):
    """
    Dùng Geocoding API để lấy tọa độ trung tâm của quận Phú Nhuận
    
    Returns:
        Tuple (lat, lng) hoặc None nếu lỗi
    """
    geocoding_url = "https://maps.googleapis.com/maps/api/geocode/json"
    
    params = {
        "address": "Phú Nhuận, Hồ Chí Minh, Việt Nam",
        "key": api_key,
        "language": "vi"
    }
    
    print("   🌍 Đang lấy tọa độ quận Phú Nhuận từ Geocoding API...")
    
    response = requests.get(geocoding_url, params=params)
    data = response.json()
    
    if data["status"] != "OK":
        print(f"      ⚠️  Lỗi Geocoding: {data.get('status')} - {data.get('error_message', '')}")
        return None
    
    results = data.get("results", [])
    if not results:
        print("      ⚠️  Không tìm thấy tọa độ Phú Nhuận")
        return None
    
    # Lấy tọa độ từ kết quả đầu tiên
    location = results[0].get("geometry", {}).get("location", {})
    lat = location.get("lat")
    lng = location.get("lng")
    
    if lat and lng:
        print(f"      ✅ Tọa độ Phú Nhuận: {lat}, {lng}")
        return (lat, lng)
    
    return None


def search_hospitals_in_phu_nhuan(api_key: str):
    """
    Tìm kiếm bệnh viện ở quận Phú Nhuận, HCM sử dụng Places Nearby Search
    Dựa trên tọa độ địa lý (chính thống hơn text search)
    
    Returns:
        Danh sách các bệnh viện (loại bỏ trùng lặp, chỉ lấy trong Phú Nhuận)
    """
    # Bước 1: Lấy tọa độ Phú Nhuận từ Geocoding API
    location = get_phu_nhuan_location(api_key)
    if not location:
        print("      ⚠️  Không thể lấy tọa độ Phú Nhuận, dừng tìm kiếm")
        return []
    
    lat, lng = location
    location_str = f"{lat},{lng}"
    
    # Bước 2: Tìm bệnh viện gần tọa độ đó bằng Nearby Search
    nearby_url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json"
    
    # Bán kính tìm kiếm: 3km (bao phủ phần lớn quận Phú Nhuận)
    radius = 3000
    
    print(f"\n   🏥 Đang tìm bệnh viện trong bán kính {radius}m từ trung tâm Phú Nhuận...")
    
    all_results = []      # Chỉ những bv có địa chỉ chứa "Phú Nhuận"
    raw_results = []      # Tất cả bv trong bán kính (để thống kê / debug)
    seen_place_ids = set()
    
    params = {
        "location": location_str,
        "radius": radius,
        "type": "hospital",
        "key": api_key,
        "language": "vi"
    }
    
    next_page_token = None
    
    # Lặp để lấy tất cả các trang kết quả
    page_num = 1
    while True:
        if next_page_token:
            params["pagetoken"] = next_page_token
            time.sleep(2)  # Đợi trước khi query next page token
            print(f"      📄 Đang lấy trang {page_num}...")
        
        response = requests.get(nearby_url, params=params)
        data = response.json()
        
        if data["status"] != "OK":
            if data["status"] != "ZERO_RESULTS":
                print(f"      ⚠️  Lỗi: {data.get('status')} - {data.get('error_message', '')}")
            break
        
        results = data.get("results", [])
        print(f"      ✅ Trang {page_num}: Tìm thấy {len(results)} bệnh viện")
        
        # Lưu tất cả kết quả trong bán kính + lọc theo địa chỉ "Phú Nhuận"
        for place in results:
            place_id = place.get("place_id")
            if place_id and place_id not in seen_place_ids:
                seen_place_ids.add(place_id)
                raw_results.append(place)

                address = place.get("vicinity", "") or place.get("formatted_address", "")
                address_lower = address.lower()
                
                # Nếu địa chỉ có chứa "Phú Nhuận" thì coi là đúng quận Phú Nhuận
                if "phú nhuận" in address_lower or "phu nhuan" in address_lower:
                    all_results.append(place)
        
        # Kiểm tra có trang tiếp theo không
        next_page_token = data.get("next_page_token")
        if not next_page_token:
            break
        
        page_num += 1

    # Log thống kê để điều tra xem filter có quá chặt không
    print(f"\n   🧪 Thống kê trong bán kính {radius}m:")
    print(f"      - Tổng số bệnh viện (không lọc địa chỉ): {len(raw_results)}")
    print(f"      - Trong đó địa chỉ có 'Phú Nhuận': {len(all_results)}")
    
    # In ra một vài địa chỉ mẫu để xem format của Google
    if raw_results:
        print(f"\n   📋 Mẫu địa chỉ từ Google (5 bệnh viện đầu tiên):")
        for i, place in enumerate(raw_results[:5], 1):
            name = place.get("name", "N/A")
            vicinity = place.get("vicinity", "")
            formatted_addr = place.get("formatted_address", "")
            address = formatted_addr or vicinity
            
            # Tính khoảng cách từ trung tâm Phú Nhuận
            place_lat = place.get("geometry", {}).get("location", {}).get("lat")
            place_lng = place.get("geometry", {}).get("location", {}).get("lng")
            distance = ""
            if place_lat and place_lng:
                dist_m = calculate_distance(lat, lng, place_lat, place_lng)
                distance = f" ({dist_m:.0f}m từ trung tâm)"
            
            print(f"      {i}. {name}")
            print(f"         📍 {address}{distance}")
            print(f"         🔍 Có 'Phú Nhuận'? {'✅' if ('phú nhuận' in address.lower() or 'phu nhuan' in address.lower()) else '❌'}")

    # Nếu không có địa chỉ nào chứa "Phú Nhuận", trả về toàn bộ để bạn tự xem
    if not all_results and raw_results:
        print("\n      ⚠️  Không có bệnh viện nào có địa chỉ chứa 'Phú Nhuận'.")
        print("         → Tạm thời trả về TOÀN BỘ bệnh viện trong bán kính để bạn xem thử địa chỉ.")
        print("         → Bạn có thể tự lọc lại dựa trên khoảng cách hoặc địa chỉ.")
        return raw_results
    
    return all_results


def find_hospital_with_most_reviews(hospitals):
    """
    Tìm bệnh viện có nhiều người đánh giá nhất
    
    Args:
        hospitals: Danh sách bệnh viện từ API
    
    Returns:
        Bệnh viện có nhiều đánh giá nhất
    """
    if not hospitals:
        return None
    
    # Sắp xếp theo số lượng đánh giá giảm dần
    hospitals_sorted = sorted(
        hospitals,
        key=lambda x: x.get("user_ratings_total", 0),
        reverse=True
    )
    
    return hospitals_sorted[0]


def main():
    """
    Hàm chính: Tìm bệnh viện có nhiều đánh giá nhất ở Phú Nhuận
    Sử dụng Geocoding API + Places Nearby Search (theo địa lý)
    """
    print("🔍 Đang tìm kiếm bệnh viện ở quận Phú Nhuận, HCM...")
    print("   (Sử dụng Geocoding API + Places Nearby Search - theo địa lý chính thống)\n")
    
    # Tìm tất cả bệnh viện
    all_hospitals = search_hospitals_in_phu_nhuan(API_KEY)
    print(f"\n📊 Tổng cộng tìm thấy {len(all_hospitals)} bệnh viện ở Phú Nhuận (sau khi loại trùng)")
    
    if not all_hospitals:
        print("\n⚠️  Không tìm thấy bệnh viện nào ở Phú Nhuận!")
        return
    
    # Sắp xếp theo số đánh giá giảm dần
    hospitals_sorted = sorted(
        all_hospitals,
        key=lambda x: x.get("user_ratings_total", 0),
        reverse=True
    )
    
    # Tìm bệnh viện có nhiều đánh giá nhất
    top_hospital = hospitals_sorted[0]
    
    # Hiển thị kết quả TOP 1
    print("\n" + "=" * 80)
    print("🏥 BỆNH VIỆN CÓ NHIỀU NGƯỜI ĐÁNH GIÁ NHẤT Ở PHÚ NHUẬN")
    print("=" * 80)
    
    # Nearby Search trả về 'vicinity' thay vì 'formatted_address'
    address = top_hospital.get('formatted_address') or top_hospital.get('vicinity', 'N/A')
    
    print(f"\n📛 Tên: {top_hospital.get('name', 'N/A')}")
    print(f"⭐ Rating: {top_hospital.get('rating', 'N/A')}/5.0")
    print(f"👥 Số đánh giá: {top_hospital.get('user_ratings_total', 0):,}")
    print(f"📍 Địa chỉ: {address}")
    print(f"🆔 Place ID: {top_hospital.get('place_id', 'N/A')}")
    
    # Hiển thị TOP 10 bệnh viện có nhiều đánh giá nhất
    print("\n" + "=" * 80)
    print(f"📋 TOP 10 BỆNH VIỆN CÓ NHIỀU ĐÁNH GIÁ NHẤT (trong {len(hospitals_sorted)} kết quả):")
    print("=" * 80)
    
    for idx, hospital in enumerate(hospitals_sorted[:10], 1):
        addr = hospital.get('formatted_address') or hospital.get('vicinity', 'N/A')
        print(f"\n{idx}. {hospital.get('name', 'N/A')}")
        print(f"   ⭐ Rating: {hospital.get('rating', 'N/A')}/5.0")
        print(f"   👥 Số đánh giá: {hospital.get('user_ratings_total', 0):,}")
        print(f"   📍 Địa chỉ: {addr}")
        print("-" * 80)
    
    # Thống kê chi phí (ước tính)
    # 1 Geocoding request + ~2-3 Nearby Search requests (tùy số trang)
    estimated_requests = 1 + 2  # 1 Geocoding + 2 Nearby Search (ước tính)
    cost = estimated_requests * 0.032  # $32/1k requests
    
    print(f"\n💰 Chi phí ước tính: ~{estimated_requests} requests = ${cost:.3f}")
    print("   (1 Geocoding + Nearby Search requests)")


if __name__ == "__main__":
    main()

