import SwiftUI
import AVKit

struct AVRoutePickerViewRepresentable: UIViewRepresentable {
    func makeUIView(context: Context) -> AVRoutePickerView {
        let picker = AVRoutePickerView()
        picker.activeTintColor = .white
        picker.tintColor = .gray
        return picker
    }

    func updateUIView(_ uiView: AVRoutePickerView, context: Context) {}
}
