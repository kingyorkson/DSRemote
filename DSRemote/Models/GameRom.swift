import Foundation

struct GameRom: Identifiable, Codable {
    let id = UUID()
    let name: String
    let fullPath: String
    let sizeFormatted: String

    enum CodingKeys: String, CodingKey {
        case name, fullPath, sizeFormatted
    }
}
