import SwiftUI

struct LayoutEditorView: View {
    @EnvironmentObject private var settings: AppSettings
    @EnvironmentObject private var layoutService: LayoutService
    @Environment(\.dismiss) private var dismiss

    @State var layout: SavedLayout
    @State private var selectedId: String?
    @State private var showColorPicker = false
    @State private var showConfirmReset = false
    @State private var scale: CGFloat = 1.0

    private let colorOptions = ["#4CAF50", "#F44336", "#2196F3", "#FFC107", "#9E9E9E",
                                "#607D8B", "#FF9800", "#9C27B0", "#E91E63", "#00BCD4",
                                "#3F51B5", "#795548", "#555555", "#FFFFFF", "#000000"]

    var body: some View {
        ZStack {
            Color(hex: "#1a1a2e").ignoresSafeArea()

            VStack(spacing: 0) {
                // Top bar
                HStack {
                    Button(action: { dismiss() }) {
                        HStack(spacing: 4) {
                            Image(systemName: "chevron.left")
                            Text("Back")
                        }
                        .foregroundColor(.gray)
                    }

                    Spacer()

                    Text(layout.name)
                        .font(.headline)
                        .foregroundColor(.white)

                    Spacer()

                    Menu {
                        Button(action: { showConfirmReset = true }) {
                            Label("Restore Default", systemImage: "arrow.counterclockwise")
                        }
                        Button(action: {
                            layoutService.duplicate(layout)
                            dismiss()
                        }) {
                            Label("Duplicate Layout", systemImage: "doc.on.doc")
                        }
                        Button(role: .destructive, action: {
                            layoutService.delete(layout)
                            dismiss()
                        }) {
                            Label("Delete Layout", systemImage: "trash")
                        }
                    } label: {
                        Image(systemName: "ellipsis.circle")
                            .foregroundColor(.gray)
                            .font(.title3)
                    }
                }
                .padding(.horizontal)
                .padding(.vertical, 8)
                .background(Color(hex: "#16213e"))

                // Canvas
                GeometryReader { geo in
                    ZStack {
                        Color(hex: "#1e1e3f")

                        // Grid lines
                        gridLines(size: geo.size)

                        // Instructions overlay
                        if layout.buttons.isEmpty {
                            Text("No buttons. Tap + to add one.")
                                .foregroundColor(.gray)
                                .font(.caption)
                        }

                        ForEach($layout.buttons) { $btn in
                            Button(action: {
                                selectedId = btn.id
                                showColorPicker = true
                            }) {
                                Text(btn.label)
                                    .font(.system(size: max(10, btn.relSize * 40 * scale)))
                                    .fontWeight(.bold)
                                    .foregroundColor(.white)
                                    .frame(
                                        width: btn.relSize * geo.size.width * scale,
                                        height: btn.relSize * geo.size.width * scale
                                    )
                                    .background(btn.color.opacity(0.85))
                                    .clipShape(Circle())
                                    .overlay(
                                        Circle()
                                            .stroke(selectedId == btn.id ? Color.white : Color.black.opacity(0.4), lineWidth: selectedId == btn.id ? 3 : 1)
                                    )
                                    .shadow(color: selectedId == btn.id ? settings.accentColor.opacity(0.6) : .clear, radius: 8)
                            }
                            .position(
                                x: btn.relX * geo.size.width,
                                y: btn.relY * geo.size.height
                            )
                            .gesture(
                                DragGesture()
                                    .onChanged { value in
                                        selectedId = btn.id
                                        btn.relX = max(0, min(1, value.location.x / geo.size.width))
                                        btn.relY = max(0, min(1, value.location.y / geo.size.height))
                                    }
                            )
                        }
                    }
                    .gesture(
                        MagnificationGesture()
                            .onChanged { value in
                                scale = value
                            }
                    )
                    .onTapGesture {
                        selectedId = nil
                    }
                }

                // Bottom bar
                HStack {
                    Button(action: { showColorPicker = true }) {
                        Image(systemName: "paintpalette")
                        Text("Color")
                    }
                    .disabled(selectedId == nil)
                    .foregroundColor(selectedId == nil ? .gray : settings.accentColor)

                    Spacer()

                    Button("Save Layout") {
                        if let idx = layoutService.layouts.firstIndex(where: { $0.id == layout.id }) {
                            layoutService.layouts[idx] = layout
                        } else {
                            layoutService.layouts.append(layout)
                        }
                        layoutService.activeLayoutId = layout.id
                        layoutService.save()
                        dismiss()
                    }
                    .fontWeight(.semibold)
                    .padding(.horizontal, 16)
                    .padding(.vertical, 8)
                    .background(settings.accentColor)
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
                .padding(.horizontal)
                .padding(.vertical, 8)
                .background(Color(hex: "#16213e"))
            }
        }
        .sheet(isPresented: $showColorPicker) {
            colorPickerSheet
        }
        .alert("Restore Default?", isPresented: $showConfirmReset) {
            Button("Cancel", role: .cancel) {}
            Button("Restore", role: .destructive) {
                layout = DefaultLayout
                layout.id = UUID()
                layout.name = "Custom"
                selectedId = nil
            }
        } message: {
            Text("This will reset all buttons to their default positions and colors.")
        }
    }

    private func gridLines(size: CGSize) -> some View {
        ZStack {
            // Vertical lines
            ForEach(0..<4, id: \.self) { i in
                Rectangle()
                    .fill(Color.white.opacity(0.06))
                    .frame(width: 1)
                    .position(x: size.width * CGFloat(i + 1) / 4, y: size.height / 2)
                    .frame(height: size.height)
            }
            // Horizontal lines
            ForEach(0..<4, id: \.self) { i in
                Rectangle()
                    .fill(Color.white.opacity(0.06))
                    .frame(height: 1)
                    .position(x: size.width / 2, y: size.height * CGFloat(i + 1) / 4)
                    .frame(width: size.width)
            }
        }
    }

    private var colorPickerSheet: some View {
        VStack(spacing: 20) {
            Text("Pick a color for \(layout.buttons.first(where: { $0.id == selectedId })?.label ?? "")")
                .font(.headline)
                .foregroundColor(.white)
                .padding(.top)

            LazyVGrid(columns: [GridItem(.adaptive(minimum: 50))], spacing: 12) {
                ForEach(colorOptions, id: \.self) { hex in
                    Circle()
                        .fill(Color(hex: hex))
                        .frame(width: 44, height: 44)
                        .overlay(Circle().stroke(Color.white.opacity(0.3), lineWidth: 1))
                        .onTapGesture {
                            if let id = selectedId, let idx = layout.buttons.firstIndex(where: { $0.id == id }) {
                                layout.buttons[idx].colorHex = hex
                            }
                            showColorPicker = false
                        }
                }
            }
            .padding()

            Button("Cancel") { showColorPicker = false }
                .foregroundColor(.gray)
                .padding(.bottom)
        }
        .padding()
        .background(Color(hex: "#1a1a2e"))
        .presentationDetents([.medium])
    }
}
