using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard
{
    private void RenderApartments()
    {
        var page = BeginPage("Dashboard", "Quản lý tòa nhà / block / tầng / căn hộ");
        int w = PageWorkWidth(1180);

        List<ApartmentDTO> apartments = new();
        List<ApartmentDTO> displayApartments = new();
        List<(int Id, string Name)> buildingItems = new();
        List<(int Id, int BuildingID, string Name)> blockItems = new();
        List<(int Id, int BlockID, int Number)> floorItems = new();
        Dictionary<int, TreeNode> apartmentNodes = new();
        ApartmentDTO? selectedApartment = null;
        bool isCreateMode = false;
        bool syncingSelection = false;
        bool suppressFilterEvents = false;
        bool suppressLocationEvents = false;
        string[] apartmentColumns =
        {
            "Mã căn hộ", "Tòa nhà", "Block", "Tầng", "Diện tích (m²)", "Loại căn hộ", "Trạng thái", "Số người tối đa"
        };
        string[] apartmentStatuses = { "Empty", "Occupied", "Renting", "Maintenance", "Locked" };

        int filterY = 80;
        int filterInputY = 103;
        int gap = 17;
        int refreshW = 112;
        int searchW = 270;
        int[] filterWidths = { 250, 238, 230, 213 };
        int x = 18;
        var buildingFilter = AddApartmentFilter(page, "Tòa nhà", "Tất cả", x, filterY, filterWidths[0]);
        x += filterWidths[0] + gap;
        var blockFilter = AddApartmentFilter(page, "Block", "Tất cả", x, filterY, filterWidths[1]);
        x += filterWidths[1] + gap;
        var floorFilter = AddApartmentFilter(page, "Tầng", "Tất cả", x, filterY, filterWidths[2]);
        x += filterWidths[2] + gap;
        var statusFilter = AddApartmentFilter(page, "Trạng thái căn hộ", "Tất cả", x, filterY, filterWidths[3]);
        x += filterWidths[3] + gap;

        var apartmentSearch = ModernUi.SearchBox("Tìm kiếm mã căn hộ...", searchW, 38);
        apartmentSearch.Location = new Point(x, filterInputY);
        page.Controls.Add(apartmentSearch);

        var refresh = ModernUi.OutlineButton("⟳  Làm mới", refreshW, 38);
        refresh.Location = new Point(w - refreshW + 18, filterInputY);
        page.Controls.Add(refresh);

        var title = ModernUi.Label("QUẢN LÝ TÒA NHÀ / BLOCK / TẦNG / CĂN HỘ", 10.5f, FontStyle.Bold, ModernUi.Blue);
        title.Location = new Point(18, 160);
        title.Size = new Size(w, 28);
        page.Controls.Add(title);

        var content = ModernUi.CardPanel();
        content.Location = new Point(18, 200);
        content.Size = new Size(w, 610);
        content.Padding = Padding.Empty;
        page.Controls.Add(content);

        var tabStrip = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(w, 50),
            BackColor = ModernUi.Header
        };
        content.Controls.Add(tabStrip);

        string activeApartmentTab = "Căn hộ";
        string[] tabNames = { "Tòa nhà", "Block", "Tầng", "Căn hộ" };

        const int innerX = 6;
        const int innerGap = 12;
        int sectionsY = 58;
        int sectionH = content.Height - sectionsY - 12;
        int treeW = Math.Min(306, Math.Max(286, (int)(w * 0.21)));
        int detailW = Math.Min(380, Math.Max(350, (int)(w * 0.27)));
        int leftW = w - innerX * 2 - detailW - treeW - innerGap * 2;
        if (leftW < 540)
        {
            leftW = 540;
            detailW = Math.Max(330, w - innerX * 2 - leftW - treeW - innerGap * 2);
        }

        var list = ModernUi.Section("Danh sách căn hộ", leftW, sectionH);
        list.Location = new Point(innerX, sectionsY);
        var grid = CreateGrid(apartmentColumns, new[] { EmptyRow(apartmentColumns.Length, "Không có căn hộ") });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, sectionH - 102);
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 37;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        int[] weights = { 88, 82, 62, 58, 96, 116, 110, 112 };
        for (int i = 0; i < grid.Columns.Count && i < weights.Length; i++)
        {
            grid.Columns[i].FillWeight = weights[i];
        }

        list.Controls.Add(grid);
        var apartmentPager = AddPaginationControls(
            list,
            18,
            sectionH - 36,
            Math.Max(220, list.Width - 320),
            sectionH - 38,
            list.Width - 80,
            sectionH - 38,
            Math.Max(190, list.Width - 360));
        content.Controls.Add(list);

        var details = ModernUi.Section("Thông tin căn hộ", detailW, 438);
        details.Height = sectionH;
        details.Location = new Point(list.Right + innerGap, sectionsY);

        int labelX = 18;
        int inputX = 148;
        int inputW = details.Width - inputX - 18;
        int fieldY = 48;
        int fieldStep = 36;
        TextBox codeInput = AddApartmentTextField(details, "Mã căn hộ", labelX, inputX, fieldY, inputW, true);
        ComboBox buildingInput = AddApartmentComboField(details, "Tòa nhà", labelX, inputX, fieldY + fieldStep, inputW, true);
        ComboBox blockInput = AddApartmentComboField(details, "Block", labelX, inputX, fieldY + fieldStep * 2, inputW, true);
        ComboBox floorInput = AddApartmentComboField(details, "Tầng", labelX, inputX, fieldY + fieldStep * 3, inputW, true);
        TextBox areaInput = AddApartmentTextField(details, "Diện tích (m²)", labelX, inputX, fieldY + fieldStep * 4, inputW, true);
        ComboBox typeInput = AddApartmentComboField(details, "Loại căn hộ", labelX, inputX, fieldY + fieldStep * 5, inputW, true);
        ComboBox statusInput = AddApartmentComboField(details, "Trạng thái", labelX, inputX, fieldY + fieldStep * 6, inputW, true);

        var maxLabel = ModernUi.Label("Số người tối đa", 8.6f, FontStyle.Regular, ModernUi.Text);
        maxLabel.Location = new Point(labelX, fieldY + fieldStep * 7);
        maxLabel.Size = new Size(inputX - labelX - 28, 30);
        details.Controls.Add(maxLabel);
        var requiredMark = ModernUi.Label("*", 8.6f, FontStyle.Bold, ModernUi.Red);
        requiredMark.Location = new Point(inputX - 22, fieldY + fieldStep * 7);
        requiredMark.Size = new Size(14, 30);
        requiredMark.TextAlign = ContentAlignment.MiddleCenter;
        details.Controls.Add(requiredMark);
        var maxResidentsInput = new NumericUpDown
        {
            Location = new Point(inputX, fieldY + fieldStep * 7),
            Size = new Size(inputW, 28),
            Minimum = 1,
            Maximum = 20,
            Font = ModernUi.Font(9f),
            BorderStyle = BorderStyle.FixedSingle
        };
        details.Controls.Add(maxResidentsInput);

        var noteLabel = ModernUi.Label("Ghi chú", 8.6f, FontStyle.Regular, ModernUi.Text);
        noteLabel.Location = new Point(labelX, fieldY + fieldStep * 8);
        noteLabel.Size = new Size(inputX - labelX - 28, 30);
        details.Controls.Add(noteLabel);
        var noteBox = new TextBox
        {
            Location = new Point(inputX, fieldY + fieldStep * 8),
            Size = new Size(inputW, 66),
            Multiline = true,
            Font = ModernUi.Font(8.8f),
            BorderStyle = BorderStyle.FixedSingle
        };
        details.Controls.Add(noteBox);

        var noteCount = ModernUi.Label("0/255", 8f, FontStyle.Regular, ModernUi.Muted);
        noteCount.Location = new Point(details.Width - 66, fieldY + fieldStep * 8 + 68);
        noteCount.Size = new Size(48, 18);
        noteCount.TextAlign = ContentAlignment.MiddleRight;
        details.Controls.Add(noteCount);

        int actionY = Math.Min(sectionH - 92, fieldY + fieldStep * 8 + 104);
        int actionGap = 10;
        int actionW = (details.Width - 36 - actionGap * 2) / 3;
        var add = ModernUi.Button("⊕  Thêm", ModernUi.Green, actionW, 34);
        add.Font = ModernUi.Font(9f, FontStyle.Bold);
        add.Location = new Point(18, actionY);
        details.Controls.Add(add);
        var edit = ModernUi.Button("✎  Sửa", ModernUi.Orange, actionW, 34);
        edit.Font = ModernUi.Font(9f, FontStyle.Bold);
        edit.Location = new Point(add.Right + actionGap, actionY);
        details.Controls.Add(edit);
        var delete = ModernUi.Button("×  Xóa", ModernUi.Red, actionW, 34);
        delete.Font = ModernUi.Font(9f, FontStyle.Bold);
        delete.Location = new Point(edit.Right + actionGap, actionY);
        details.Controls.Add(delete);

        int saveW = (details.Width - 46) / 2;
        var save = ModernUi.Button("▣  Lưu", ModernUi.Blue, saveW, 36);
        save.Font = ModernUi.Font(9f, FontStyle.Bold);
        save.Location = new Point(18, actionY + 46);
        details.Controls.Add(save);
        var cancel = ModernUi.Button("×  Hủy", Color.FromArgb(107, 118, 132), saveW, 36);
        cancel.Font = ModernUi.Font(9f, FontStyle.Bold);
        cancel.Location = new Point(save.Right + 10, actionY + 46);
        details.Controls.Add(cancel);
        content.Controls.Add(details);

        var tree = ModernUi.Section("Sơ đồ block - tầng - căn hộ", treeW, sectionH);
        tree.Location = new Point(details.Right + innerGap, sectionsY);
        var treeView = new TreeView
        {
            Location = new Point(18, 44),
            Size = new Size(tree.Width - 36, sectionH - 62),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = ModernUi.Text,
            Font = ModernUi.Font(9.2f),
            HideSelection = false,
            HotTracking = true,
            Indent = 20,
            ItemHeight = 24,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true
        };
        tree.Controls.Add(treeView);
        content.Controls.Add(tree);

        void BuildApartmentTabs()
        {
            tabStrip.Controls.Clear();

            for (int i = 0; i < tabNames.Length; i++)
            {
                string tabName = tabNames[i];

                AddApartmentTab(tabStrip, tabName, tabName == activeApartmentTab, i * 112, (_, _) =>
                {
                    ShowApartmentTab(tabName);
                });
            }
        }

        void ShowApartmentTab(string tabName)
        {
            activeApartmentTab = tabName;
            BuildApartmentTabs();

            bool isApartmentTab = tabName == "Căn hộ";

            list.Visible = isApartmentTab;
            details.Visible = isApartmentTab;

            if (isApartmentTab)
            {
                tree.Visible = true;

                int newDetailW = Math.Min(380, Math.Max(350, (int)(w * 0.30)));
                int newTreeW = Math.Min(300, Math.Max(260, (int)(w * 0.22)));
                int newListW = w - innerX * 2 - newDetailW - newTreeW - innerGap * 2;

                if (newListW < 520)
                {
                    newTreeW = 0;
                    newListW = w - innerX * 2 - newDetailW - innerGap;
                    tree.Visible = false;
                }

                list.Location = new Point(innerX, sectionsY);
                list.Size = new Size(newListW, sectionH);
                grid.Size = new Size(list.Width - 24, sectionH - 102);

                details.Location = new Point(list.Right + innerGap, sectionsY);
                details.Size = new Size(newDetailW, sectionH);

                tree.Location = new Point(details.Right + innerGap, sectionsY);
                tree.Size = new Size(newTreeW, sectionH);
                treeView.Size = new Size(Math.Max(120, tree.Width - 36), sectionH - 62);
            }
            else
            {
                list.Visible = false;
                details.Visible = false;
                tree.Visible = true;

                tree.Location = new Point(innerX, sectionsY);
                tree.Size = new Size(w - innerX * 2, sectionH);

                treeView.Location = new Point(18, 44);
                treeView.Size = new Size(tree.Width - 36, sectionH - 62);
            }
        }

        ShowApartmentTab("Căn hộ");

        typeInput.Items.Clear();
        typeInput.AddOption("Studio", "Studio");
        typeInput.AddOption("1 PN - 1 WC", "1BR");
        typeInput.AddOption("2 PN - 1 WC", "2BR");
        typeInput.AddOption("3 PN - 2 WC", "3BR");
        typeInput.AddOption("4 PN - 2 WC", "4BR");
        typeInput.AddOption("Penthouse", "Penthouse");
        if (typeInput.Items.Count > 0)
        {
            typeInput.SelectedIndex = 0;
        }

        statusInput.Items.Clear();
        statusInput.AddOption("Đang trống", "Empty");
        statusInput.AddOption("Đang sử dụng", "Occupied");
        statusInput.AddOption("Đang thuê", "Renting");
        statusInput.AddOption("Bảo trì", "Maintenance");
        statusInput.AddOption("Đang khóa", "Locked");
        statusInput.SelectValue("Empty");
        codeInput.MaxLength = 20;
        noteBox.MaxLength = 255;

        string FloorLabel(int? floorNumber)
            => floorNumber.HasValue && floorNumber.Value > 0 ? $"Tầng {floorNumber.Value:00}" : "-";

        string DbApartmentStatus(string? status)
        {
            return (status ?? string.Empty).Trim() switch
            {
                "Đang trống" => "Empty",
                "Đang sử dụng" => "Occupied",
                "Đang thuê" => "Renting",
                "Bảo trì" => "Maintenance",
                "Đang khóa" => "Locked",
                "" => "Empty",
                var other => other
            };
        }

        bool TryParseArea(string text, out decimal area)
        {
            text = (text ?? string.Empty).Trim();
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out area) ||
                   decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out area) ||
                   decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out area);
        }

        void SetFilterItems(ComboBox combo, IEnumerable<string> items, string preferred = "Tất cả")
        {
            string[] options = items
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase)
                .Prepend("Tất cả")
                .ToArray();

            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(options.Cast<object>().ToArray());
            combo.EndUpdate();

            int index = Array.FindIndex(options, item => string.Equals(item, preferred, StringComparison.OrdinalIgnoreCase));
            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        HashSet<int> GetMatchingBuildingIds(string? buildingText)
        {
            if (string.IsNullOrWhiteSpace(buildingText) || string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return buildingItems.Select(item => item.Id).ToHashSet();
            }

            return buildingItems
                .Where(item => string.Equals(BuildingShort(item.Name), buildingText, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Id)
                .ToHashSet();
        }

        HashSet<int> GetMatchingBlockIds(string? buildingText, string? blockText)
        {
            IEnumerable<(int Id, int BuildingID, string Name)> query = blockItems;
            if (!string.IsNullOrWhiteSpace(buildingText) && !string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                HashSet<int> buildingIds = GetMatchingBuildingIds(buildingText);
                query = query.Where(item => buildingIds.Contains(item.BuildingID));
            }

            if (!string.IsNullOrWhiteSpace(blockText) && !string.Equals(blockText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item => string.Equals(BlockShort(item.Name), blockText, StringComparison.OrdinalIgnoreCase));
            }

            return query.Select(item => item.Id).ToHashSet();
        }

        int? FindBuildingIdByLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var match = buildingItems.FirstOrDefault(item => string.Equals(BuildingShort(item.Name), label, StringComparison.OrdinalIgnoreCase));
            return match.Id > 0 ? match.Id : null;
        }

        int? FindBlockIdByLabel(string? label, int? buildingId = null)
        {
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            IEnumerable<(int Id, int BuildingID, string Name)> query = blockItems;
            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(item => item.BuildingID == buildingId.Value);
            }

            var match = query.FirstOrDefault(item => string.Equals(BlockShort(item.Name), label, StringComparison.OrdinalIgnoreCase));
            return match.Id > 0 ? match.Id : null;
        }

        int? FindFloorIdByLabel(string? label, int? blockId = null)
        {
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            IEnumerable<(int Id, int BlockID, int Number)> query = floorItems;
            if (blockId.HasValue && blockId.Value > 0)
            {
                query = query.Where(item => item.BlockID == blockId.Value);
            }

            var match = query.FirstOrDefault(item => string.Equals(FloorLabel(item.Number), label, StringComparison.OrdinalIgnoreCase));
            return match.Id > 0 ? match.Id : null;
        }

        void PopulateBuildingInput(int? preferredBuildingId = null)
        {
            buildingInput.BeginUpdate();
            buildingInput.Items.Clear();
            foreach (var building in buildingItems.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                buildingInput.AddOption(BuildingShort(building.Name), building.Id);
            }

            buildingInput.EndUpdate();
            if (preferredBuildingId.HasValue)
            {
                buildingInput.SelectValue(preferredBuildingId.Value);
            }

            if (buildingInput.SelectedIndex < 0 && buildingInput.Items.Count > 0)
            {
                buildingInput.SelectedIndex = 0;
            }
        }

        void PopulateBlockInput(int? buildingId, int? preferredBlockId = null)
        {
            blockInput.BeginUpdate();
            blockInput.Items.Clear();
            IEnumerable<(int Id, int BuildingID, string Name)> query = blockItems;
            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(item => item.BuildingID == buildingId.Value);
            }

            foreach (var block in query.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                blockInput.AddOption($"Block {BlockShort(block.Name)}", block.Id);
            }

            blockInput.EndUpdate();
            if (preferredBlockId.HasValue)
            {
                blockInput.SelectValue(preferredBlockId.Value);
            }

            if (blockInput.SelectedIndex < 0 && blockInput.Items.Count > 0)
            {
                blockInput.SelectedIndex = 0;
            }
        }

        void PopulateFloorInput(int? blockId, int? preferredFloorId = null)
        {
            floorInput.BeginUpdate();
            floorInput.Items.Clear();
            IEnumerable<(int Id, int BlockID, int Number)> query = floorItems;
            if (blockId.HasValue && blockId.Value > 0)
            {
                query = query.Where(item => item.BlockID == blockId.Value);
            }

            foreach (var floor in query.OrderBy(item => item.Number))
            {
                floorInput.AddOption(floor.Number.ToString("00", CultureInfo.InvariantCulture), floor.Id);
            }

            floorInput.EndUpdate();
            if (preferredFloorId.HasValue)
            {
                floorInput.SelectValue(preferredFloorId.Value);
            }

            if (floorInput.SelectedIndex < 0 && floorInput.Items.Count > 0)
            {
                floorInput.SelectedIndex = 0;
            }
        }

        void SetLocationSelection(int? buildingId, int? blockId, int? floorId)
        {
            suppressLocationEvents = true;
            try
            {
                PopulateBuildingInput(buildingId);
                int selectedBuildingId = buildingInput.GetSelectedValueInt();
                PopulateBlockInput(selectedBuildingId > 0 ? selectedBuildingId : buildingId, blockId);
                int selectedBlockId = blockInput.GetSelectedValueInt();
                PopulateFloorInput(selectedBlockId > 0 ? selectedBlockId : blockId, floorId);
            }
            finally
            {
                suppressLocationEvents = false;
            }
        }

        void LoadReferenceData()
        {
            apartments = ApartmentDAL.GetAllApartments();

            buildingItems = new List<(int Id, string Name)>();
            foreach (dynamic building in BuildingDAL.GetAllBuildings())
            {
                int id = Convert.ToInt32(building.BuildingID, CultureInfo.InvariantCulture);
                string name = Display(building.BuildingName, "");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    buildingItems.Add((id, name));
                }
            }

            blockItems = new List<(int Id, int BuildingID, string Name)>();
            foreach (var building in buildingItems)
            {
                foreach (dynamic block in BlockDAL.GetBlocksByBuilding(building.Id))
                {
                    int blockId = Convert.ToInt32(block.BlockID, CultureInfo.InvariantCulture);
                    int blockBuildingId = Convert.ToInt32(block.BuildingID, CultureInfo.InvariantCulture);
                    string blockName = Display(block.BlockName, "");
                    if (!string.IsNullOrWhiteSpace(blockName))
                    {
                        blockItems.Add((blockId, blockBuildingId, blockName));
                    }
                }
            }

            floorItems = new List<(int Id, int BlockID, int Number)>();
            foreach (var block in blockItems)
            {
                foreach (dynamic floor in FloorDAL.GetFloorsByBlock(block.Id))
                {
                    int floorId = Convert.ToInt32(floor.FloorID, CultureInfo.InvariantCulture);
                    int floorBlockId = Convert.ToInt32(floor.BlockID, CultureInfo.InvariantCulture);
                    int floorNumber = Convert.ToInt32(floor.FloorNumber, CultureInfo.InvariantCulture);
                    floorItems.Add((floorId, floorBlockId, floorNumber));
                }
            }
        }

        void RefreshFilterOptions(string? preferredBuilding = null, string? preferredBlock = null, string? preferredFloor = null, string? preferredStatus = null)
        {
            suppressFilterEvents = true;
            try
            {
                string buildingText = preferredBuilding ?? buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
                string blockText = preferredBlock ?? blockFilter.SelectedItem?.ToString() ?? "Tất cả";
                string floorText = preferredFloor ?? floorFilter.SelectedItem?.ToString() ?? "Tất cả";
                string statusText = preferredStatus ?? statusFilter.SelectedItem?.ToString() ?? "Tất cả";

                SetFilterItems(buildingFilter, buildingItems.Select(item => BuildingShort(item.Name)), buildingText);
                buildingText = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";

                IEnumerable<(int Id, int BuildingID, string Name)> blockQuery = blockItems;
                if (!string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
                {
                    HashSet<int> buildingIds = GetMatchingBuildingIds(buildingText);
                    blockQuery = blockQuery.Where(item => buildingIds.Contains(item.BuildingID));
                }

                SetFilterItems(blockFilter, blockQuery.Select(item => BlockShort(item.Name)), blockText);
                blockText = blockFilter.SelectedItem?.ToString() ?? "Tất cả";

                IEnumerable<(int Id, int BlockID, int Number)> floorQuery = floorItems;
                if (!string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(blockText, "Tất cả", StringComparison.OrdinalIgnoreCase))
                {
                    HashSet<int> blockIds = GetMatchingBlockIds(buildingText, blockText);
                    floorQuery = floorQuery.Where(item => blockIds.Contains(item.BlockID));
                }

                SetFilterItems(floorFilter, floorQuery.Select(item => FloorLabel(item.Number)), floorText);
                SetFilterItems(statusFilter, apartmentStatuses.Select(ViStatus), statusText);
            }
            finally
            {
                suppressFilterEvents = false;
            }
        }

        List<ApartmentDTO> FilterApartments()
        {
            string buildingText = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            string blockText = blockFilter.SelectedItem?.ToString() ?? "Tất cả";
            string floorText = floorFilter.SelectedItem?.ToString() ?? "Tất cả";
            string statusText = statusFilter.SelectedItem?.ToString() ?? "Tất cả";
            string searchText = apartmentSearch.Text.Trim();

            IEnumerable<ApartmentDTO> filtered = apartments;
            if (!string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(BuildingShort(apartment.BuildingName), buildingText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(blockText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(BlockShort(apartment.BlockName), blockText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(floorText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(FloorLabel(apartment.FloorNumber), floorText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(statusText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(ViStatus(apartment.Status), statusText, StringComparison.OrdinalIgnoreCase));
            }

            if (searchText.Length > 0)
            {
                filtered = filtered.Where(apartment =>
                {
                    string haystack = string.Join(' ',
                        Display(apartment.ApartmentCode, string.Empty),
                        Display(apartment.BuildingName, string.Empty),
                        BuildingShort(apartment.BuildingName),
                        Display(apartment.BlockName, string.Empty),
                        BlockShort(apartment.BlockName),
                        apartment.FloorNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                        FloorLabel(apartment.FloorNumber),
                        Display(apartment.ApartmentType, string.Empty),
                        ApartmentTypeText(apartment.ApartmentType),
                        Display(apartment.Status, string.Empty),
                        ViStatus(apartment.Status),
                        Display(apartment.Note, string.Empty));

                    return haystack.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
                });
            }

            return filtered
                .OrderBy(apartment => Display(apartment.BuildingName, string.Empty), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(apartment => Display(apartment.BlockName, string.Empty), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(apartment => apartment.FloorNumber ?? 0)
                .ThenBy(apartment => Display(apartment.ApartmentCode, string.Empty), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        var apartmentPagination = new PaginationState();

        void PopulateApartmentGrid(IReadOnlyList<ApartmentDTO> source)
        {
            grid.SuspendLayout();
            grid.DataSource = null;
            grid.Rows.Clear();

            // If the grid was originally created with a DataSource the auto-generated columns
            // may have been removed when DataSource was cleared. Ensure columns exist before
            // adding rows manually to avoid InvalidOperationException.
            if (grid.Columns.Count == 0)
            {
                foreach (var col in apartmentColumns)
                {
                    grid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        HeaderText = col,
                        DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
                    });
                }
                grid.ColumnHeadersHeight = 38;
                grid.RowTemplate.Height = 37;
            }

            if (source.Count == 0)
            {
                grid.Rows.Add(EmptyRow(apartmentColumns.Length, "Không có căn hộ phù hợp"));
                grid.ClearSelection();
                grid.ResumeLayout();
                return;
            }

            foreach (var apartment in source)
            {
                int rowIndex = grid.Rows.Add(
                    Display(apartment.ApartmentCode),
                    BuildingShort(apartment.BuildingName),
                    BlockShort(apartment.BlockName),
                    apartment.FloorNumber?.ToString("00", CultureInfo.InvariantCulture) ?? "-",
                    apartment.Area.ToString("N2", CultureInfo.InvariantCulture),
                    ApartmentTypeText(apartment.ApartmentType),
                    ViStatus(apartment.Status),
                    apartment.MaxResidents);

                grid.Rows[rowIndex].Tag = apartment;
            }

            grid.ClearSelection();
            grid.ResumeLayout();
        }

        ApartmentDTO? FirstApartmentInNode(TreeNode node)
        {
            if (node.Tag is ApartmentDTO apartment)
            {
                return apartment;
            }

            foreach (TreeNode child in node.Nodes)
            {
                var found = FirstApartmentInNode(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        void PopulateApartmentTree(IReadOnlyList<ApartmentDTO> source)
        {
            apartmentNodes = new Dictionary<int, TreeNode>();
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            var root = new TreeNode("▣  Dự án khu chung cư");
            treeView.Nodes.Add(root);

            foreach (var buildingGroup in source.GroupBy(apartment => BuildingShort(apartment.BuildingName)))
            {
                var buildingNode = new TreeNode($"▰  {buildingGroup.Key}");
                root.Nodes.Add(buildingNode);

                foreach (var blockGroup in buildingGroup.GroupBy(apartment => BlockShort(apartment.BlockName)))
                {
                    var blockNode = new TreeNode($"▰  Block {blockGroup.Key}");
                    buildingNode.Nodes.Add(blockNode);

                    foreach (var floorGroup in blockGroup.GroupBy(apartment => apartment.FloorNumber ?? 0).OrderByDescending(group => group.Key))
                    {
                        var floorNode = new TreeNode($"▦  Tầng {floorGroup.Key:00}");
                        blockNode.Nodes.Add(floorNode);

                        foreach (var apartment in floorGroup.OrderBy(item => Display(item.ApartmentCode, string.Empty), StringComparer.CurrentCultureIgnoreCase))
                        {
                            var apartmentNode = new TreeNode($"▣  {Display(apartment.ApartmentCode)}")
                            {
                                Tag = apartment
                            };
                            floorNode.Nodes.Add(apartmentNode);
                            apartmentNodes[apartment.ApartmentID] = apartmentNode;
                        }
                    }
                }
            }

            root.Expand();
            if (root.Nodes.Count > 0)
            {
                root.Nodes[0].Expand();
                if (root.Nodes[0].Nodes.Count > 0)
                {
                    root.Nodes[0].Nodes[0].Expand();
                }
            }

            treeView.EndUpdate();
        }

        void PrepareCreateMode()
        {
            isCreateMode = true;
            codeInput.Clear();
            areaInput.Clear();
            noteBox.Clear();
            noteCount.Text = "0/255";
            maxResidentsInput.Value = 4;
            if (typeInput.Items.Count > 0)
            {
                typeInput.SelectedIndex = 0;
            }

            statusInput.SelectValue("Empty");

            int? preferredBuildingId = FindBuildingIdByLabel(buildingFilter.SelectedItem?.ToString()) ??
                                       selectedApartment?.BuildingID ??
                                       (buildingItems.Count > 0 ? buildingItems[0].Id : null);
            int? preferredBlockId = FindBlockIdByLabel(blockFilter.SelectedItem?.ToString(), preferredBuildingId) ??
                                    (selectedApartment?.BuildingID == preferredBuildingId ? selectedApartment?.BlockID : null);
            int? preferredFloorId = FindFloorIdByLabel(floorFilter.SelectedItem?.ToString(), preferredBlockId) ??
                                    (selectedApartment?.BlockID == preferredBlockId ? selectedApartment?.FloorID : null);
            SetLocationSelection(preferredBuildingId, preferredBlockId, preferredFloorId);

            syncingSelection = true;
            try
            {
                grid.ClearSelection();
                treeView.SelectedNode = null;
            }
            finally
            {
                syncingSelection = false;
            }

            codeInput.Focus();
        }

        void SelectApartment(ApartmentDTO? apartment, bool fromTree = false)
        {
            if (apartment == null)
            {
                return;
            }

            selectedApartment = apartment;
            isCreateMode = false;
            codeInput.Text = Display(apartment.ApartmentCode, "");
            SetLocationSelection(apartment.BuildingID, apartment.BlockID, apartment.FloorID);
            areaInput.Text = apartment.Area.ToString("N2", CultureInfo.InvariantCulture);
            typeInput.SelectValue(apartment.ApartmentType);
            statusInput.SelectValue(apartment.Status);
            maxResidentsInput.Value = Math.Min(maxResidentsInput.Maximum, Math.Max(maxResidentsInput.Minimum, apartment.MaxResidents <= 0 ? 1 : apartment.MaxResidents));
            noteBox.Text = Display(apartment.Note, "");
            noteCount.Text = $"{Math.Min(noteBox.TextLength, 255)}/255";

            syncingSelection = true;
            try
            {
                grid.ClearSelection();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Tag is ApartmentDTO rowApartment && rowApartment.ApartmentID == apartment.ApartmentID)
                    {
                        row.Selected = true;
                        grid.CurrentCell = row.Cells[0];
                        break;
                    }
                }

                if (!fromTree && apartmentNodes.TryGetValue(apartment.ApartmentID, out var node))
                {
                    treeView.SelectedNode = node;
                    node.EnsureVisible();
                }
            }
            finally
            {
                syncingSelection = false;
            }
        }

        bool ValidateApartmentForm(out int floorId, out decimal area, out string apartmentType, out string apartmentStatus, out string apartmentCode, out string? note)
        {
            floorId = 0;
            area = 0;
            apartmentType = string.Empty;
            apartmentStatus = string.Empty;
            apartmentCode = codeInput.Text.Trim();
            note = string.IsNullOrWhiteSpace(noteBox.Text) ? null : noteBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(apartmentCode))
            {
                MessageBox.Show(this, "Mã căn hộ không được để trống.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                codeInput.Focus();
                return false;
            }

            if (apartmentCode.Length > 20)
            {
                MessageBox.Show(this, "Mã căn hộ tối đa 20 ký tự.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                codeInput.Focus();
                return false;
            }

            floorId = floorInput.GetSelectedValueInt();
            if (floorId <= 0)
            {
                MessageBox.Show(this, "Bạn chưa chọn tầng hợp lệ.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryParseArea(areaInput.Text, out area) || area <= 0 || area > 1000)
            {
                MessageBox.Show(this, "Diện tích phải là số lớn hơn 0 và không vượt quá 1000 m².",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                areaInput.Focus();
                return false;
            }

            apartmentType = typeInput.GetSelectedValueString();
            if (string.IsNullOrWhiteSpace(apartmentType))
            {
                MessageBox.Show(this, "Bạn chưa chọn loại căn hộ.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            apartmentStatus = DbApartmentStatus(statusInput.GetSelectedText());
            return true;
        }

        void RefreshApartmentView(int? preferredApartmentId = null, bool keepCreateMode = false)
        {
            displayApartments = FilterApartments();
            if (preferredApartmentId.HasValue)
            {
                int preferredIndex = displayApartments.FindIndex(apartment => apartment.ApartmentID == preferredApartmentId.Value);
                if (preferredIndex >= 0)
                {
                    apartmentPagination.CurrentPage = FindPageForIndex(preferredIndex, apartmentPagination.PageSize);
                }
            }

            PopulateApartmentGrid(Paginate(displayApartments, apartmentPagination));
            PopulateApartmentTree(displayApartments);
            UpdatePaginationControls(apartmentPagination, apartmentPager, "căn hộ");

            if (keepCreateMode)
            {
                PrepareCreateMode();
                return;
            }

            if (preferredApartmentId.HasValue)
            {
                var preferred = displayApartments.FirstOrDefault(apartment => apartment.ApartmentID == preferredApartmentId.Value);
                if (preferred != null)
                {
                    SelectApartment(preferred);
                    return;
                }
            }

            if (selectedApartment != null)
            {
                var current = displayApartments.FirstOrDefault(apartment => apartment.ApartmentID == selectedApartment.ApartmentID);
                if (current != null)
                {
                    SelectApartment(current);
                    return;
                }
            }

            if (displayApartments.Count > 0)
            {
                SelectApartment(displayApartments[0]);
                return;
            }

            PrepareCreateMode();
        }

        void RefreshApartmentPage(bool keepCreateMode = false)
        {
            displayApartments = FilterApartments();
            IReadOnlyList<ApartmentDTO> pageApartments = Paginate(displayApartments, apartmentPagination);
            PopulateApartmentGrid(pageApartments);
            PopulateApartmentTree(displayApartments);
            UpdatePaginationControls(apartmentPagination, apartmentPager, "cÄƒn há»™");

            if (keepCreateMode)
            {
                PrepareCreateMode();
                return;
            }

            ApartmentDTO? selectionTarget = null;
            if (selectedApartment != null)
            {
                selectionTarget = pageApartments.FirstOrDefault(apartment => apartment.ApartmentID == selectedApartment.ApartmentID);
            }

            selectionTarget ??= pageApartments.FirstOrDefault();

            if (selectionTarget != null)
            {
                SelectApartment(selectionTarget);
                return;
            }

            PrepareCreateMode();
        }

        void ReloadApartmentData(int? preferredApartmentId = null, bool keepCreateMode = false)
        {
            string currentBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            string currentBlock = blockFilter.SelectedItem?.ToString() ?? "Tất cả";
            string currentFloor = floorFilter.SelectedItem?.ToString() ?? "Tất cả";
            string currentStatus = statusFilter.SelectedItem?.ToString() ?? "Tất cả";

            LoadReferenceData();
            RefreshFilterOptions(currentBuilding, currentBlock, currentFloor, currentStatus);
            RefreshApartmentView(preferredApartmentId, keepCreateMode);
        }

        void SaveApartment()
        {
            if (!ValidateApartmentForm(out int floorId, out decimal area, out string apartmentType, out string apartmentStatus, out string apartmentCode, out string? note))
            {
                return;
            }

            if (isCreateMode || selectedApartment == null)
            {
                var createResult = ApartmentBLL.CreateApartment(apartmentCode, floorId, area, apartmentType, (int)maxResidentsInput.Value, note);
                if (!createResult.Success)
                {
                    MessageBox.Show(this, createResult.Message,
                        "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!string.Equals(apartmentStatus, "Empty", StringComparison.OrdinalIgnoreCase))
                {
                    var statusResult = ApartmentBLL.UpdateApartmentStatus(createResult.ApartmentID, apartmentStatus);
                    if (!statusResult.Success)
                    {
                        MessageBox.Show(this,
                            $"Đã tạo căn hộ nhưng chưa cập nhật được trạng thái.\n{statusResult.Message}",
                            "Quản lý căn hộ",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }

                AuditLogDAL.LogAction(_session?.UserID, "Create_Apartment", "Apartment", createResult.ApartmentID, $"Tạo căn hộ: {apartmentCode}");
                MessageBox.Show(this, "Đã thêm căn hộ thành công.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadApartmentData(createResult.ApartmentID);
                return;
            }

            var updateResult = ApartmentBLL.UpdateApartment(selectedApartment.ApartmentID, floorId, apartmentCode, area, apartmentType, (int)maxResidentsInput.Value, note);
            if (!updateResult.Success)
            {
                MessageBox.Show(this, updateResult.Message,
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.Equals(apartmentStatus, selectedApartment.Status, StringComparison.OrdinalIgnoreCase))
            {
                var statusResult = ApartmentBLL.UpdateApartmentStatus(selectedApartment.ApartmentID, apartmentStatus);
                if (!statusResult.Success)
                {
                    MessageBox.Show(this, statusResult.Message,
                        "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            AuditLogDAL.LogAction(_session?.UserID, "Update_Apartment", "Apartment", selectedApartment.ApartmentID, $"Cập nhật căn hộ: {apartmentCode}");
            MessageBox.Show(this, "Đã lưu thay đổi căn hộ.",
                "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadApartmentData(selectedApartment.ApartmentID);
        }

        void DeleteApartment()
        {
            if (selectedApartment == null || isCreateMode)
            {
                MessageBox.Show(this, "Bạn chưa chọn căn hộ để xóa.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this,
                    $"Xóa căn hộ `{selectedApartment.ApartmentCode}`?",
                    "Quản lý căn hộ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var result = ApartmentBLL.DeleteApartment(selectedApartment.ApartmentID);
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message,
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Delete_Apartment", "Apartment", selectedApartment.ApartmentID, $"Xóa căn hộ: {selectedApartment.ApartmentCode}");
            MessageBox.Show(this, "Đã xóa căn hộ.",
                "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectedApartment = null;
            ReloadApartmentData();
        }

        buildingInput.SelectedIndexChanged += (_, _) =>
        {
            if (suppressLocationEvents)
            {
                return;
            }

            suppressLocationEvents = true;
            try
            {
                PopulateBlockInput(buildingInput.GetSelectedValueInt());
                PopulateFloorInput(blockInput.GetSelectedValueInt());
            }
            finally
            {
                suppressLocationEvents = false;
            }
        };

        blockInput.SelectedIndexChanged += (_, _) =>
        {
            if (suppressLocationEvents)
            {
                return;
            }

            suppressLocationEvents = true;
            try
            {
                PopulateFloorInput(blockInput.GetSelectedValueInt());
            }
            finally
            {
                suppressLocationEvents = false;
            }
        };

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is ApartmentDTO apartment)
            {
                SelectApartment(apartment);
            }
        };

        treeView.AfterSelect += (_, e) =>
        {
            if (syncingSelection)
            {
                return;
            }

            SelectApartment(FirstApartmentInNode(e.Node), fromTree: true);
        };

        apartmentPager.FirstButton.Click += (_, _) =>
        {
            apartmentPagination.MoveToFirstPage();
            RefreshApartmentPage(isCreateMode);
        };
        apartmentPager.PreviousButton.Click += (_, _) =>
        {
            apartmentPagination.MoveToPreviousPage();
            RefreshApartmentPage(isCreateMode);
        };
        apartmentPager.NextButton.Click += (_, _) =>
        {
            apartmentPagination.MoveToNextPage();
            RefreshApartmentPage(isCreateMode);
        };
        apartmentPager.LastButton.Click += (_, _) =>
        {
            apartmentPagination.MoveToLastPage();
            RefreshApartmentPage(isCreateMode);
        };
        apartmentPager.PageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            apartmentPagination.SetPageSize(ParsePageSize(apartmentPager.PageSizeCombo.SelectedItem, apartmentPagination.PageSize));
            RefreshApartmentPage(isCreateMode);
        };

        apartmentSearch.TextChanged += (_, _) =>
        {
            apartmentPagination.MoveToFirstPage();
            RefreshApartmentPage(isCreateMode);
        };
        buildingFilter.SelectedIndexChanged += (_, _) =>
        {
            if (suppressFilterEvents)
            {
                return;
            }

            RefreshFilterOptions(buildingFilter.SelectedItem?.ToString(), blockFilter.SelectedItem?.ToString(), floorFilter.SelectedItem?.ToString(), statusFilter.SelectedItem?.ToString());
            apartmentPagination.MoveToFirstPage();
            RefreshApartmentPage(isCreateMode);
        };
        blockFilter.SelectedIndexChanged += (_, _) =>
        {
            if (suppressFilterEvents)
            {
                return;
            }

            RefreshFilterOptions(buildingFilter.SelectedItem?.ToString(), blockFilter.SelectedItem?.ToString(), floorFilter.SelectedItem?.ToString(), statusFilter.SelectedItem?.ToString());
            apartmentPagination.MoveToFirstPage();
            RefreshApartmentPage(isCreateMode);
        };
        floorFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                apartmentPagination.MoveToFirstPage();
                RefreshApartmentPage(isCreateMode);
            }
        };
        statusFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                apartmentPagination.MoveToFirstPage();
                RefreshApartmentPage(isCreateMode);
            }
        };

        refresh.Click += (_, _) => ReloadApartmentData(selectedApartment?.ApartmentID, isCreateMode);
        add.Click += (_, _) => PrepareCreateMode();
        edit.Click += (_, _) =>
        {
            if (selectedApartment == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn căn hộ để sửa.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var refreshedApartment = ApartmentDAL.GetApartmentByID(selectedApartment.ApartmentID) ?? selectedApartment;
            SelectApartment(refreshedApartment);
            codeInput.Focus();
            codeInput.SelectAll();
        };
        delete.Click += (_, _) => DeleteApartment();
        save.Click += (_, _) => SaveApartment();
        cancel.Click += (_, _) =>
        {
            if (selectedApartment != null)
            {
                ReloadApartmentData(selectedApartment.ApartmentID);
            }
            else
            {
                PrepareCreateMode();
            }
        };
        noteBox.TextChanged += (_, _) => noteCount.Text = $"{Math.Min(noteBox.TextLength, 255)}/255";

        ReloadApartmentData();
        if (ConsumeQuickAction("apartments", "add"))
        {
            PrepareCreateMode();
        }
    }

    private static ComboBox AddApartmentFilter(Control parent, string label, string selected, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(width, 20);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(new[] { selected }, width);
        combo.Location = new Point(x, y + 22);
        combo.Height = 38;
        parent.Controls.Add(combo);
        return combo;
    }

    private static ComboBox AddResidentFilterCombo(Control parent, string label, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(width, 20);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(new[] { "Tất cả" }, width);
        combo.Location = new Point(x, y + 22);
        combo.Height = 32;
        parent.Controls.Add(combo);
        return combo;
    }

    private static void AddApartmentTab(Control parent, string text, bool active, int x, EventHandler click)
    {
        var tab = ModernUi.Label(
            text,
            9.2f,
            active ? FontStyle.Bold : FontStyle.Regular,
            active ? ModernUi.Blue : ModernUi.Text);

        tab.Location = new Point(x, 0);
        tab.Size = new Size(112, 50);
        tab.TextAlign = ContentAlignment.MiddleCenter;
        tab.Cursor = Cursors.Hand;
        tab.BackColor = active ? Color.White : ModernUi.Header;
        tab.Click += click;
        parent.Controls.Add(tab);

        if (active)
        {
            var line = new Panel
            {
                BackColor = ModernUi.Blue,
                Location = new Point(x, 48),
                Size = new Size(112, 2),
                Cursor = Cursors.Hand
            };

            line.Click += click;
            parent.Controls.Add(line);
        }
    }

    private static TextBox AddApartmentTextField(Control parent, string label, int labelX, int inputX, int y, int inputW, bool required)
    {
        AddApartmentFieldLabel(parent, label, labelX, inputX, y, required);
        var input = ModernUi.TextBox("", inputW);
        input.Location = new Point(inputX, y);
        input.Height = 28;
        input.Font = ModernUi.Font(9f);
        parent.Controls.Add(input);
        return input;
    }

    private static ComboBox AddApartmentComboField(Control parent, string label, int labelX, int inputX, int y, int inputW, bool required)
    {
        AddApartmentFieldLabel(parent, label, labelX, inputX, y, required);
        var input = ModernUi.ComboBox(Array.Empty<string>(), inputW);
        input.Location = new Point(inputX, y);
        input.Height = 28;
        input.Font = ModernUi.Font(9f);
        parent.Controls.Add(input);
        return input;
    }

    private static void AddApartmentFieldLabel(Control parent, string label, int x, int inputX, int y, bool required)
    {
        var lbl = ModernUi.Label(label, 8.6f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(inputX - x - 28, 30);
        parent.Controls.Add(lbl);

        if (required)
        {
            var mark = ModernUi.Label("*", 8.6f, FontStyle.Bold, ModernUi.Red);
            mark.Location = new Point(inputX - 22, y);
            mark.Size = new Size(14, 30);
            mark.TextAlign = ContentAlignment.MiddleCenter;
            parent.Controls.Add(mark);
        }
    }

    private static Label AddApartmentPager(Control parent, int sectionH, int width, int total)
    {
        var totalLabel = ModernUi.Label($"Tổng số: {total:N0} căn hộ", 8.8f, FontStyle.Bold, ModernUi.Blue);
        totalLabel.Location = new Point(18, sectionH - 38);
        totalLabel.Size = new Size(180, 28);
        parent.Controls.Add(totalLabel);

        string[] pages = { "|<", "<", "1", "2", "3", "...", "13", ">", ">|" };
        int buttonW = 31;
        int startX = Math.Max(210, width - 402);
        for (int i = 0; i < pages.Length; i++)
        {
            var pageButton = ModernUi.OutlineButton(pages[i], buttonW, 30);
            pageButton.Font = ModernUi.Font(8.6f, i == 2 ? FontStyle.Bold : FontStyle.Regular);
            pageButton.Location = new Point(startX + i * (buttonW + 6), sectionH - 38);
            if (i == 2)
            {
                pageButton.BackColor = ModernUi.Blue;
                pageButton.ForeColor = Color.White;
            }
            parent.Controls.Add(pageButton);
        }

        var pageSize = ModernUi.ComboBox(new[] { "10", "20", "50" }, 66);
        pageSize.Location = new Point(width - 80, sectionH - 38);
        parent.Controls.Add(pageSize);
        return totalLabel;
    }

    private static string BuildingShort(string? value)
    {
        string text = Display(value);
        return text.Replace("Tòa nhà", "Tòa", StringComparison.OrdinalIgnoreCase)
                   .Replace("Toà nhà", "Toà", StringComparison.OrdinalIgnoreCase);
    }

    private static string BlockShort(string? value)
    {
        string text = Display(value);
        return text.Replace("Block", "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static string ApartmentTypeText(string? type)
    {
        return (type ?? string.Empty).Trim() switch
        {
            "Studio" => "Studio",
            "1BR" or "1 phòng ngủ" => "1 PN - 1 WC",
            "2BR" or "2 phòng ngủ" => "2 PN - 1 WC",
            "3BR" or "3 phòng ngủ" => "3 PN - 2 WC",
            "4BR" or "4 phòng ngủ" => "4 PN - 2 WC",
            "Penthouse" => "Penthouse",
            "" => "-",
            var other => other
        };
    }
}
