import UIKit

enum InputType: String, Codable {
    case buttonDown
    case buttonUp
    case touchDown
    case touchMove
    case touchUp
    case joystickMove
    case dPadPress
}

struct InputEvent: Codable {
    let type: InputType
    let args: [Float]
}

struct PCDeviceInfo: Codable {
    var deviceName: String = UIDevice.current.name
    var platform: String = "iPhone"
    var osVersion: String = UIDevice.current.systemVersion
}
