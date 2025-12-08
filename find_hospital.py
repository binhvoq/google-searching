"""
Tìm địa điểm theo vùng và từ khóa tại Việt Nam
Sử dụng Geocoding Viewport để tự động tính bán kính tìm kiếm
Hỗ trợ: User nhập vùng (Đà Lạt, Quận 8, Vũng Tàu...) và keyword (khách sạn, cafe làm việc...)
"""

import requests
import time
import math
import sys
import io

# Fix encoding cho Windows
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

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


def get_location_and_radius(api_key: str, area_query: str):
    """
    Lấy tọa độ tâm VÀ tự động tính bán kính dựa trên Viewport của Google
    
    Args:
        api_key: Google Maps API key
        area_query: Tên vùng (ví dụ: "Đà Lạt", "Quận 8, HCM", "Vũng Tàu")
    
    Returns:
        Tuple ((lat, lng), radius) hoặc (None, None) nếu lỗi
    """
    geocoding_url = "https://maps.googleapis.com/maps/api/geocode/json"
    
    # Luôn thêm ", Việt Nam" để đảm bảo tìm trong VN
    full_address = f"{area_query}, Việt Nam"
    
    params = {
        "address": full_address,
        "key": api_key,
        "language": "vi"
    }
    
    print(f"   🌍 Đang phân tích địa chỉ '{area_query}'...")
    print(f"      📝 Địa chỉ đầy đủ: '{full_address}'")
    
    try:
        response = requests.get(geocoding_url, params=params, timeout=10)
        response.raise_for_status()  # Ném lỗi nếu HTTP status code không OK
        data = response.json()
    except requests.exceptions.RequestException as e:
        print(f"      ❌ Lỗi kết nối: {str(e)}")
        return None, None
    except ValueError as e:
        print(f"      ❌ Lỗi parse JSON: {str(e)}")
        return None, None
    
    status = data.get("status", "UNKNOWN")
    
    # Xử lý các trường hợp lỗi khác nhau
    if status == "OK":
        pass  # Tiếp tục xử lý bên dưới
    elif status == "ZERO_RESULTS":
        print(f"      ⚠️  Không tìm thấy địa chỉ '{area_query}'")
        print(f"      💡 Gợi ý: Thử địa chỉ khác hoặc kiểm tra chính tả")
        return None, None
    elif status == "OVER_QUERY_LIMIT":
        print(f"      ❌ Đã vượt quá giới hạn API. Vui lòng thử lại sau.")
        return None, None
    elif status == "REQUEST_DENIED":
        error_msg = data.get("error_message", "Không có thông tin")
        print(f"      ❌ API request bị từ chối: {error_msg}")
        print(f"      💡 Kiểm tra API key và quyền truy cập")
        return None, None
    elif status == "INVALID_REQUEST":
        print(f"      ❌ Yêu cầu không hợp lệ. Kiểm tra lại địa chỉ.")
        return None, None
    else:
        error_msg = data.get("error_message", "Không có thông tin")
        print(f"      ⚠️  Lỗi Geocoding: {status}")
        if error_msg:
            print(f"      📋 Chi tiết: {error_msg}")
        return None, None
    
    results = data.get("results", [])
    if not results:
        print(f"      ⚠️  Không tìm thấy tọa độ cho '{area_query}'")
        return None, None
    
    # Lấy kết quả đầu tiên (phổ biến nhất)
    result = results[0]
    geometry = result.get("geometry", {})
    location = geometry.get("location", {})
    lat = location.get("lat")
    lng = location.get("lng")
    
    if not lat or not lng:
        print("      ⚠️  Không có tọa độ hợp lệ")
        return None, None
    
    print(f"      ✅ Tọa độ trung tâm: {lat}, {lng}")
    
    # Lấy Viewport để tính bán kính tự động
    viewport = geometry.get("viewport", {})
    if viewport:
        # Lấy góc Đông Bắc (northeast) - điểm xa nhất trong viewport
        ne = viewport.get("northeast", {})
        ne_lat = ne.get("lat")
        ne_lng = ne.get("lng")
        
        if ne_lat and ne_lng:
            # Tính khoảng cách từ Tâm đến góc Đông Bắc
            # Nhân với 1.2 để đảm bảo bao phủ toàn bộ vùng (có margin)
            radius = calculate_distance(lat, lng, ne_lat, ne_lng) * 1.2
            
            # Giới hạn radius tối đa 50km (tránh quá lớn)
            radius = min(radius, 50000)
            # Giới hạn radius tối thiểu 2km (đảm bảo tìm được kết quả)
            radius = max(radius, 2000)
            
            print(f"      ✅ Phát hiện vùng rộng. Tự động set bán kính: {radius/1000:.1f} km")
        else:
            radius = 5000  # Mặc định 5km nếu không có northeast
            print(f"      ⚠️  Không có viewport đầy đủ, dùng bán kính mặc định: 5km")
    else:
        radius = 5000  # Mặc định 5km nếu không có viewport
        print(f"      ⚠️  Không có Viewport, dùng bán kính mặc định: 5km")
    
    return (lat, lng), radius


def normalize_area_name(area: str):
    """
    Chuẩn hóa tên vùng để so sánh (bỏ dấu, lowercase)
    Ví dụ: "Phú Nhuận" -> "phu nhuan", "Quận 8" -> "quan 8"
    """
    # Bỏ dấu tiếng Việt (đơn giản)
    replacements = {
        'á': 'a', 'à': 'a', 'ả': 'a', 'ã': 'a', 'ạ': 'a',
        'ă': 'a', 'ắ': 'a', 'ằ': 'a', 'ẳ': 'a', 'ẵ': 'a', 'ặ': 'a',
        'â': 'a', 'ấ': 'a', 'ầ': 'a', 'ẩ': 'a', 'ẫ': 'a', 'ậ': 'a',
        'é': 'e', 'è': 'e', 'ẻ': 'e', 'ẽ': 'e', 'ẹ': 'e',
        'ê': 'e', 'ế': 'e', 'ề': 'e', 'ể': 'e', 'ễ': 'e', 'ệ': 'e',
        'í': 'i', 'ì': 'i', 'ỉ': 'i', 'ĩ': 'i', 'ị': 'i',
        'ó': 'o', 'ò': 'o', 'ỏ': 'o', 'õ': 'o', 'ọ': 'o',
        'ô': 'o', 'ố': 'o', 'ồ': 'o', 'ổ': 'o', 'ỗ': 'o', 'ộ': 'o',
        'ơ': 'o', 'ớ': 'o', 'ờ': 'o', 'ở': 'o', 'ỡ': 'o', 'ợ': 'o',
        'ú': 'u', 'ù': 'u', 'ủ': 'u', 'ũ': 'u', 'ụ': 'u',
        'ư': 'u', 'ứ': 'u', 'ừ': 'u', 'ử': 'u', 'ữ': 'u', 'ự': 'u',
        'ý': 'y', 'ỳ': 'y', 'ỷ': 'y', 'ỹ': 'y', 'ỵ': 'y',
        'đ': 'd'
    }
    
    text = area.lower()
    for old, new in replacements.items():
        text = text.replace(old, new)
    
    return text.strip()


def is_place_in_area(place: dict, area_query: str, center_lat: float, center_lng: float, max_distance: float):
    """
    Kiểm tra xem địa điểm có nằm trong vùng tìm kiếm không
    
    Args:
        place: Đối tượng địa điểm từ API
        area_query: Tên vùng user nhập (ví dụ: "Phú Nhuận", "Quận 8")
        center_lat, center_lng: Tọa độ trung tâm vùng
        max_distance: Khoảng cách tối đa (radius) - đã tính từ viewport
    
    Returns:
        True nếu địa điểm thuộc vùng, False nếu không
    """
    # Lấy địa chỉ của place
    address = place.get("vicinity", "") or place.get("formatted_address", "")
    address_lower = address.lower()
    
    # Chuẩn hóa tên vùng để so sánh
    area_normalized = normalize_area_name(area_query)
    area_keywords = area_normalized.split()  # Tách thành từng từ
    
    # Kiểm tra 1: Địa chỉ có chứa tên vùng không?
    address_normalized = normalize_area_name(address)
    has_area_in_address = any(keyword in address_normalized for keyword in area_keywords if len(keyword) > 2)
    
    # Kiểm tra 2: Khoảng cách từ trung tâm
    place_geometry = place.get("geometry", {})
    place_location = place_geometry.get("location", {})
    place_lat = place_location.get("lat")
    place_lng = place_location.get("lng")
    
    within_distance = True
    if place_lat and place_lng:
        distance = calculate_distance(center_lat, center_lng, place_lat, place_lng)
        within_distance = distance <= max_distance
    
    # Địa điểm thuộc vùng nếu: (có tên vùng trong địa chỉ) HOẶC (nằm trong bán kính)
    # Ưu tiên địa chỉ hơn (chính xác hơn)
    if has_area_in_address:
        return True
    
    # Nếu không có tên vùng trong địa chỉ, kiểm tra khoảng cách
    return within_distance


def search_places(api_key: str, area: str, keyword: str = ""):
    """
    Tìm kiếm địa điểm theo vùng và từ khóa
    
    Args:
        api_key: Google Maps API key
        area: Tên vùng (ví dụ: "Đà Lạt", "Quận 8, HCM", "Vũng Tàu")
        keyword: Từ khóa tìm kiếm (ví dụ: "khách sạn", "cafe làm việc", "bệnh viện")
                 Nếu để trống, sẽ tìm tất cả địa điểm trong vùng
    
    Returns:
        Danh sách các địa điểm tìm được (đã lọc theo vùng)
    """
    print(f"\n🔍 Tìm kiếm: '{keyword or 'Tất cả địa điểm'}' tại '{area}'")
    print("   (Sử dụng Geocoding Viewport + Places Nearby Search)\n")
    
    # Bước 1: Lấy tọa độ & Radius tự động từ Viewport
    location, radius = get_location_and_radius(api_key, area)
    if not location:
        print("      ⚠️  Không thể lấy tọa độ, dừng tìm kiếm")
        return []
    
    lat, lng = location
    location_str = f"{lat},{lng}"
    
    # Bước 2: Tìm kiếm với Places Nearby Search
    nearby_url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json"
    
    params = {
        "location": location_str,
        "radius": int(radius),  # Phải là số nguyên
        "key": api_key,
        "language": "vi"
    }
    
    # Ưu tiên keyword nếu có (tìm linh hoạt), nếu không thì không set type/keyword (tìm tất cả)
    if keyword:
        params["keyword"] = keyword
        print(f"   🔍 Tìm kiếm theo từ khóa: '{keyword}'")
    else:
        print(f"   🔍 Tìm kiếm tất cả địa điểm trong vùng")
    
    all_results = []      # Chỉ những địa điểm thuộc vùng
    raw_results = []      # Tất cả địa điểm trong bán kính (để thống kê)
    seen_place_ids = set()
    
    print(f"\n   🚀 Đang tìm trong bán kính {radius/1000:.1f}km từ trung tâm...")
    
    next_page_token = None
    page_num = 1
    
    # Lặp để lấy tất cả các trang kết quả
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
        print(f"      ✅ Trang {page_num}: Tìm thấy {len(results)} địa điểm")
        
        # Lưu tất cả kết quả trong bán kính + lọc theo vùng
        for place in results:
            place_id = place.get("place_id")
            if place_id and place_id not in seen_place_ids:
                seen_place_ids.add(place_id)
                raw_results.append(place)
                
                # Kiểm tra xem địa điểm có thuộc vùng không
                if is_place_in_area(place, area, lat, lng, radius):
                    all_results.append(place)
        
        # Kiểm tra có trang tiếp theo không
        next_page_token = data.get("next_page_token")
        if not next_page_token:
            break
        
        page_num += 1
    
    # Log thống kê
    print(f"\n   🧪 Thống kê:")
    print(f"      - Tổng số địa điểm trong bán kính: {len(raw_results)}")
    print(f"      - Trong đó thuộc vùng '{area}': {len(all_results)}")
    
    # Nếu không có kết quả nào thuộc vùng, trả về toàn bộ (có thể do filter quá chặt)
    if not all_results and raw_results:
        print(f"\n      ⚠️  Không có địa điểm nào khớp với vùng '{area}'.")
        print("         → Trả về TOÀN BỘ địa điểm trong bán kính để bạn xem thử.")
        return raw_results
    
    return all_results


def format_address(place: dict, search_area: str = ""):
    """
    Format địa chỉ đầy đủ, rõ ràng hơn
    
    Args:
        place: Đối tượng địa điểm từ API
        search_area: Vùng tìm kiếm (để thêm vào nếu địa chỉ ngắn)
    
    Returns:
        Địa chỉ đã được format đầy đủ
    """
    # Ưu tiên formatted_address (thường đầy đủ hơn)
    address = place.get("formatted_address") or place.get("vicinity", "N/A")
    
    if address == "N/A":
        return "N/A"
    
    # Kiểm tra xem địa chỉ đã đầy đủ chưa (có chứa tên thành phố/tỉnh)
    # Nếu địa chỉ ngắn (chỉ có phường/đường), thêm thông tin vùng vào
    address_lower = address.lower()
    
    # Danh sách từ khóa cho biết địa chỉ đã đầy đủ
    full_address_indicators = [
        "việt nam", "vietnam", "viet nam",
        "hồ chí minh", "ho chi minh", "hcm", "tp.hcm",
        "hà nội", "ha noi", "hn",
        "đà lạt", "da lat", "lâm đồng", "lam dong",
        "vũng tàu", "vung tau", "bà rịa", "ba ria",
        "đà nẵng", "da nang",
        "cần thơ", "can tho",
        "huế", "hue", "thừa thiên", "thua thien"
    ]
    
    # Kiểm tra xem địa chỉ đã có thông tin thành phố/tỉnh chưa
    is_full_address = any(indicator in address_lower for indicator in full_address_indicators)
    
    # Nếu địa chỉ ngắn và có thông tin vùng tìm kiếm, thêm vào
    if not is_full_address and search_area:
        # Thêm vùng tìm kiếm vào cuối địa chỉ
        address = f"{address}, {search_area}, Việt Nam"
    
    return address


def display_results(places: list, keyword: str = "", search_area: str = ""):
    """
    Hiển thị kết quả tìm kiếm
    
    Args:
        places: Danh sách địa điểm
        keyword: Từ khóa tìm kiếm (để hiển thị)
        search_area: Vùng tìm kiếm (để format địa chỉ đầy đủ)
    """
    if not places:
        print("\n⚠️  Không tìm thấy địa điểm nào!")
        return
    
    # Sắp xếp theo số đánh giá giảm dần
    places_sorted = sorted(
        places,
        key=lambda x: x.get("user_ratings_total", 0),
        reverse=True
    )
    
    print("\n" + "=" * 80)
    print(f"📋 KẾT QUẢ TÌM KIẾM ({len(places_sorted)} địa điểm)")
    print("=" * 80)
    
    # Hiển thị TOP 10
    top_n = min(10, len(places_sorted))
    for idx, place in enumerate(places_sorted[:top_n], 1):
        name = place.get("name", "N/A")
        rating = place.get("rating", "N/A")
        reviews = place.get("user_ratings_total", 0)
        
        # Format địa chỉ đầy đủ
        address = format_address(place, search_area)
        
        place_id = place.get("place_id", "N/A")
        
        # Lấy loại địa điểm
        types = place.get("types", [])
        place_type = ", ".join([t.replace("_", " ").title() for t in types[:3]])
        
        print(f"\n{idx}. {name}")
        print(f"   ⭐ Rating: {rating}/5.0" if rating != "N/A" else "   ⭐ Rating: Chưa có")
        print(f"   👥 Số đánh giá: {reviews:,}" if reviews > 0 else "   👥 Số đánh giá: Chưa có")
        print(f"   📍 Địa chỉ: {address}")
        if place_type:
            print(f"   🏷️  Loại: {place_type}")
        print(f"   🆔 Place ID: {place_id}")
        print("-" * 80)


def main():
    """
    Hàm chính: Demo cách sử dụng
    """
    # ====== CẤU HÌNH TÌM KIẾM ======
    # Thay đổi 2 dòng này để tìm kiếm khác:
    AREA = "Gò Vấp"  # Vùng tìm kiếm (ví dụ: "Đà Lạt", "Quận 8, HCM", "Vũng Tàu")
    KEYWORD = "thịt nướng"    # Từ khóa (ví dụ: "khách sạn", "cafe làm việc", "công viên nước")
                             # Để trống "" nếu muốn tìm tất cả địa điểm
    
    # ====== CHẠY TÌM KIẾM ======
    results = search_places(API_KEY, AREA, KEYWORD)
    display_results(results, KEYWORD, AREA)
    
    # Thống kê chi phí
    estimated_requests = 1 + 2  # 1 Geocoding + ~2 Nearby Search (ước tính)
    cost_geocoding = 1 * 0.005  # $5/1k requests
    cost_places = 2 * 0.032      # $32/1k requests
    total_cost = cost_geocoding + cost_places
    
    print(f"\n💰 Chi phí ước tính: ~${total_cost:.4f}")
    print("   (1 Geocoding + Nearby Search requests)")


if __name__ == "__main__":
    main()
