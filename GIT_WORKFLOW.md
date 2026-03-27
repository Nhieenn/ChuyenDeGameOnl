# Git Workflow & Standards - Project: AntiGravity

## 1. Cấu trúc Nhánh (Branching Strategy)
- `main` / `master`: Nhánh production, chứa code ổn định nhất, build cho Windows/Android.
- `develop`: Nhánh tích hợp chính. Code đang phát triển sẽ được tập hợp tại đây.
- `feature/<tên-tính-năng>`: Nhánh phát triển tính năng mới (VD: `feature/player-movement`, `feature/object-pooling`).
- `bugfix/<tên-lỗi>`: Nhánh sửa lỗi phát sinh trong quá trình dev từ nhánh `develop`.
- `hotfix/<tên-lỗi>`: Nhánh sửa lỗi khẩn cấp trực tiếp từ nhánh `main`.

## 2. Quy tắc Commit (Conventional Commits)
Sử dụng các tiền tố sau khi viết commit message:
- `feat:` Thêm tính năng mới (VD: `feat: add player jump logic with fusion`).
- `fix:` Sửa lỗi (VD: `fix: resolve timeout disconnect issue`).
- `refactor:` Tối ưu code, cấu trúc lại code mà không thay đổi logic hiện tại.
- `chore:` Thay đổi cấu hình, build process, hoặc các tác vụ không liên quan trực tiếp đến code game.
- `docs:` Cập nhật tài liệu (VD: `docs: update .cursorrules`).

## 3. Quy trình làm việc cơ bản (Workflow)
1. Cập nhật nhánh develop: `git checkout develop` -> `git pull origin develop`
2. Tạo nhánh mới: `git checkout -b feature/<tên-tính-năng>`
3. Code và commit thường xuyên theo quy tắc ở mục 2.
4. Push nhánh lên remote: `git push origin feature/<tên-tính-năng>`
5. Tạo Pull Request (PR) để merge vào `develop`.
6. Xóa nhánh feature ở local và remote sau khi đã merge thành công.

## 4. Xung đột (Merge Conflicts) trong Unity
- **Cảnh báo:** Tuyệt đối không nhiều người cùng sửa một file `.scene` hoặc `.prefab` cùng một lúc.
- Luôn chia nhỏ UI hoặc Object thành các Prefab riêng biệt để tránh conflict.
- Nếu xảy ra conflict ở file scene/prefab, ưu tiên sử dụng công cụ UnityYAMLMerge.