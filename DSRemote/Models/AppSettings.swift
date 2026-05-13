import SwiftUI

class AppSettings: ObservableObject {
    @Published var accentColor: Color {
        didSet {
            UserDefaults.standard.set(accentColor.toHex(), forKey: "accentColor")
        }
    }

    @Published var lastHost: String {
        didSet {
            UserDefaults.standard.set(lastHost, forKey: "lastHost")
        }
    }

    @Published var lastPort: Int {
        didSet {
            UserDefaults.standard.set(lastPort, forKey: "lastPort")
        }
    }

    init() {
        let savedHex = UserDefaults.standard.string(forKey: "accentColor") ?? "#32CD32"
        accentColor = Color(hex: savedHex)
        lastHost = UserDefaults.standard.string(forKey: "lastHost") ?? ""
        lastPort = UserDefaults.standard.integer(forKey: "lastPort")
        if lastPort == 0 { lastPort = 9876 }
    }
}

extension Color {
    func toHex() -> String {
        let uiColor = UIColor(self)
        var r: CGFloat = 0; var g: CGFloat = 0; var b: CGFloat = 0; var a: CGFloat = 0
        uiColor.getRed(&r, green: &g, blue: &b, alpha: &a)
        return String(format: "#%02X%02X%02X", Int(r * 255), Int(g * 255), Int(b * 255))
    }

    init(hex: String) {
        let hex = hex.trimmingCharacters(in: CharacterSet.alphanumerics.inverted)
        var int: UInt64 = 0
        Scanner(string: hex).scanHexInt64(&int)
        let r, g, b, a: UInt64
        switch hex.count {
        case 6:
            (r, g, b, a) = ((int >> 16) & 0xFF, (int >> 8) & 0xFF, int & 0xFF, 255)
        case 8:
            (r, g, b, a) = ((int >> 24) & 0xFF, (int >> 16) & 0xFF, (int >> 8) & 0xFF, int & 0xFF)
        default:
            self = .gray
            return
        }
        self.init(.sRGB, red: Double(r) / 255, green: Double(g) / 255, blue: Double(b) / 255, opacity: Double(a) / 255)
    }
}
