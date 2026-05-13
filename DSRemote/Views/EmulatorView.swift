import SwiftUI
import UIKit

struct EmulatorView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @EnvironmentObject private var layoutService: LayoutService
    @Binding var topScreen: UIImage?
    @Binding var bottomScreen: UIImage?
    let onPowerOff: () -> Void
    let onDisconnect: () -> Void

    @State private var showPowerAlert = false

    var body: some View {
        GeometryReader { geo in
            let isLandscape = geo.size.width > geo.size.height

            VStack(spacing: 0) {
                topBar
                if isLandscape {
                    landscapeBody(geo: geo)
                } else {
                    portraitBody(geo: geo)
                }
            }
        }
        .background(Color(hex: "#1a1a2e"))
        .background(settings.accentColor.opacity(0.15))
        .ignoresSafeArea(.keyboard)
    }

    // MARK: - Top Bar
    private var topBar: some View {
        HStack {
            Button(action: { showPowerAlert = true }) {
                Image(systemName: "power")
                    .font(.caption)
                    .foregroundColor(.red)
                    .padding(6)
                    .background(Color.red.opacity(0.15))
                    .clipShape(Circle())
            }
            .alert("Quit Game?", isPresented: $showPowerAlert) {
                Button("Cancel", role: .cancel) {}
                Button("Quit", role: .destructive) {
                    network.sendStopEmulation()
                    onPowerOff()
                }
            } message: {
                Text("This will stop the emulator and return to game selection.")
            }

            Text(network.connectionType.rawValue)
                .font(.caption2)
                .fontWeight(.bold)
                .foregroundColor(network.connectionType == .usb ? settings.accentColor : .blue)
                .padding(.horizontal, 6)
                .padding(.vertical, 2)
                .background((network.connectionType == .usb ? settings.accentColor : Color.blue).opacity(0.15))
                .cornerRadius(4)
                .padding(.leading, 4)

            Spacer()

            Text(network.emulatorName)
                .font(.caption)
                .foregroundColor(.gray)

            Spacer()

            Button(action: onDisconnect) {
                HStack(spacing: 3) {
                    Image(systemName: "power").font(.caption2)
                    Text("Disconnect").font(.caption2)
                }
                .padding(.horizontal, 10)
                .padding(.vertical, 6)
                .background(Color(hex: "#e94560"))
                .foregroundColor(.white)
                .cornerRadius(8)
            }
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 4)
    }

    // MARK: - Portrait
    private func portraitBody(geo: GeometryProxy) -> some View {
        VStack(spacing: 4) {
            ScreenView(image: $topScreen, label: "Top")
                .frame(height: geo.size.height * 0.42)

            ScreenView(image: $bottomScreen, label: "Bottom")
                .frame(height: geo.size.height * 0.30)
                .overlay(TouchSurfaceView(
                    onTouchDown: { pt in network.sendInput(.touchDown, args: [Float(pt.x), Float(pt.y)]) },
                    onTouchMove: { pt in network.sendInput(.touchMove, args: [Float(pt.x), Float(pt.y)]) },
                    onTouchUp: { network.sendInput(.touchUp, args: []) }
                ))

            controlZone(containerSize: geo.size)
        }
    }

    // MARK: - Landscape
    private func landscapeBody(geo: GeometryProxy) -> some View {
        HStack(spacing: 0) {
            leftControls(containerSize: geo.size)
                .frame(width: geo.size.width * 0.22)

            VStack(spacing: 2) {
                ScreenView(image: $topScreen, label: "Top")
                    .frame(height: geo.size.height * 0.40)
                ScreenView(image: $bottomScreen, label: "Bottom")
                    .frame(height: geo.size.height * 0.28)
                    .overlay(TouchSurfaceView(
                        onTouchDown: { pt in network.sendInput(.touchDown, args: [Float(pt.x), Float(pt.y)]) },
                        onTouchMove: { pt in network.sendInput(.touchMove, args: [Float(pt.x), Float(pt.y)]) },
                        onTouchUp: { network.sendInput(.touchUp, args: []) }
                    ))
            }
            .frame(width: geo.size.width * 0.56)

            rightControls(containerSize: geo.size)
                .frame(width: geo.size.width * 0.22)
        }
    }

    // MARK: - Controls (Portrait)
    private func controlZone(containerSize: CGSize) -> some View {
        GeometryReader { geo in
            ZStack {
                ForEach(layoutService.activeLayout.buttons) { btn in
                    controlButton(btn, containerSize: geo.size)
                }
            }
        }
        .padding(.horizontal, 8)
        .padding(.bottom, 4)
    }

    // MARK: - Controls (Landscape)
    private func leftControls(containerSize: CGSize) -> some View {
        GeometryReader { geo in
            let leftButtons = layoutService.activeLayout.buttons.filter {
                $0.id == "dpad" || $0.id == "joystick" || $0.id == "l"
            }
            ZStack {
                ForEach(leftButtons) { btn in
                    controlButton(btn, containerSize: geo.size)
                }
            }
        }
    }

    private func rightControls(containerSize: CGSize) -> some View {
        GeometryReader { geo in
            let rightButtons = layoutService.activeLayout.buttons.filter {
                $0.id != "dpad" && $0.id != "joystick" && $0.id != "l"
            }
            ZStack {
                ForEach(rightButtons) { btn in
                    controlButton(btn, containerSize: geo.size)
                }
            }
        }
    }

    // MARK: - Button Renderer
    private func controlButton(_ config: ButtonConfig, containerSize: CGSize) -> some View {
        let sizeMultiplier: CGFloat = 1.4
        let w = containerSize.width * config.relSize * sizeMultiplier
        let h = containerSize.height * config.relSize * sizeMultiplier

        return Group {
            if config.id == "dpad" {
                compactDPad(size: min(w, h) * 0.8)
            } else if config.id == "joystick" {
                JoystickView { x, y in
                    network.sendInput(.joystickMove, args: [x, y])
                }
                .frame(width: min(w, h), height: min(w, h))
            } else {
                let btnId = buttonId(for: config.id)
                Text(config.label)
                    .font(.system(size: max(10, config.relSize * 30 * sizeMultiplier)))
                    .fontWeight(.bold)
                    .foregroundColor(.white)
                    .frame(width: min(w, h), height: min(w, h))
                    .background(config.color.opacity(0.4))
                    .clipShape(Circle())
                    .overlay(Circle().stroke(config.color, lineWidth: 3))
                    .contentShape(Circle())
                    .onLongPressGesture(minimumDuration: 0, pressing: { isPressing in
                        if isPressing {
                            network.sendInput(.buttonDown, args: [Float(btnId)])
                        } else {
                            network.sendInput(.buttonUp, args: [Float(btnId)])
                        }
                    }, perform: {})
            }
        }
        .position(x: config.relX * containerSize.width, y: config.relY * containerSize.height)
    }

    // MARK: - D-Pad
    private func compactDPad(size: CGFloat) -> some View {
        VStack(spacing: 2) {
            CompactDPadBtn(direction: .up, label: "\u{25B2}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [0]) })
            HStack(spacing: 2) {
                CompactDPadBtn(direction: .left, label: "\u{25C0}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [2]) })
                Color.clear.frame(width: size * 0.3, height: size * 0.3)
                CompactDPadBtn(direction: .right, label: "\u{25B6}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [3]) })
            }
            CompactDPadBtn(direction: .down, label: "\u{25BC}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [1]) })
        }
    }

    private func buttonId(for id: String) -> Int {
        switch id {
        case "a": return 0
        case "b": return 1
        case "x": return 2
        case "y": return 3
        case "l": return 4
        case "r": return 5
        case "start": return 6
        case "select": return 7
        default: return 0
        }
    }
}

// MARK: - Screen View with Upscaling
struct ScreenView: View {
    @Binding var image: UIImage?
    let label: String

    var body: some View {
        ZStack {
            Color.black
            if let img = image {
                Image(uiImage: img)
                    .resizable()
                    .interpolation(.high)
                    .aspectRatio(contentMode: .fit)
            } else {
                VStack {
                    Image(systemName: "rectangle.split.2x1").font(.largeTitle).foregroundColor(.gray)
                    Text(label).font(.caption).foregroundColor(.gray)
                }
            }
        }
        .cornerRadius(8)
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(hex: "#0f3460"), lineWidth: 1))
        .padding(.horizontal, 4)
    }
}

// MARK: - Touch Surface
struct TouchSurfaceView: UIViewRepresentable {
    let onTouchDown: (CGPoint) -> Void
    let onTouchMove: (CGPoint) -> Void
    let onTouchUp: () -> Void

    func makeUIView(context: Context) -> UIView {
        let view = UIView()
        view.backgroundColor = .clear
        let pan = UIPanGestureRecognizer(target: context.coordinator, action: #selector(Coordinator.handlePan(_:)))
        let tap = UITapGestureRecognizer(target: context.coordinator, action: #selector(Coordinator.handleTap(_:)))
        pan.maximumNumberOfTouches = 1
        view.addGestureRecognizer(pan)
        view.addGestureRecognizer(tap)
        return view
    }

    func updateUIView(_ uiView: UIView, context: Context) {}

    func makeCoordinator() -> Coordinator {
        Coordinator(onTouchDown: onTouchDown, onTouchMove: onTouchMove, onTouchUp: onTouchUp)
    }

    class Coordinator: NSObject {
        let onTouchDown: (CGPoint) -> Void
        let onTouchMove: (CGPoint) -> Void
        let onTouchUp: () -> Void

        init(onTouchDown: @escaping (CGPoint) -> Void, onTouchMove: @escaping (CGPoint) -> Void, onTouchUp: @escaping () -> Void) {
            self.onTouchDown = onTouchDown
            self.onTouchMove = onTouchMove
            self.onTouchUp = onTouchUp
        }

        private func normalizedLocation(in view: UIView, from gesture: UIGestureRecognizer) -> CGPoint {
            let point = gesture.location(in: view)
            return CGPoint(x: point.x / view.bounds.width, y: point.y / view.bounds.height)
        }

        @objc func handlePan(_ gesture: UIPanGestureRecognizer) {
            guard let view = gesture.view else { return }
            let pt = normalizedLocation(in: view, from: gesture)
            switch gesture.state {
            case .began: onTouchDown(pt)
            case .changed: onTouchMove(pt)
            case .ended, .cancelled: onTouchUp()
            default: break
            }
        }

        @objc func handleTap(_ gesture: UITapGestureRecognizer) {
            guard let view = gesture.view else { return }
            let pt = normalizedLocation(in: view, from: gesture)
            onTouchDown(pt)
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { self.onTouchUp() }
        }
    }
}

// MARK: - Compact D-Pad Button
struct CompactDPadBtn: View {
    let direction: DPadDirection
    let label: String
    let size: CGFloat
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Text(label)
                .font(.system(size: size * 0.5))
                .foregroundColor(.white)
                .frame(width: size, height: size)
                .background(Color(hex: "#333333"))
                .cornerRadius(4)
        }
    }
}
