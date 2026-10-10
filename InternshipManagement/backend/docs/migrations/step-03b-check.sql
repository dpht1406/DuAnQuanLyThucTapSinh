-- Q1. Thống kê số đơn theo từng trạng thái đang lưu trong PlacementRequests.
SELECT
    Status,
    COUNT(*) AS RequestCount
FROM PlacementRequests
GROUP BY Status
ORDER BY Status;

-- Q2. Phân loại đơn Approved; sinh viên xóa mềm được xếp trước mọi nhóm CompanyId.
SELECT
    COUNT(CASE
        WHEN s.IsDeleted = 1 THEN 1
    END) AS SoftDeletedStudentCount,
    COUNT(CASE
        WHEN s.IsDeleted = 0
            AND s.CompanyId = pr.CompanyId THEN 1
    END) AS MatchingCompanyCount,
    COUNT(CASE
        WHEN s.IsDeleted = 0
            AND s.CompanyId IS NOT NULL
            AND s.CompanyId <> pr.CompanyId THEN 1
    END) AS DifferentCompanyCount,
    COUNT(CASE
        WHEN s.IsDeleted = 0
            AND s.CompanyId IS NULL THEN 1
    END) AS NoCompanyCount
FROM PlacementRequests AS pr
INNER JOIN Students AS s ON s.Id = pr.StudentId
WHERE pr.Status = 'Approved';

-- Q3. Liệt kê đơn Approved còn hiệu lực có CompanyId khác đơn hoặc sinh viên chưa có CompanyId.
SELECT
    pr.Id AS PlacementRequestId,
    pr.StudentId,
    pr.CompanyId AS RequestCompanyId,
    s.CompanyId AS StudentCompanyId,
    pr.JobPositionId AS RequestJobPositionId,
    s.JobPositionId AS StudentJobPositionId,
    s.Status AS StudentStatus
FROM PlacementRequests AS pr
INNER JOIN Students AS s ON s.Id = pr.StudentId
WHERE pr.Status = 'Approved'
    AND s.IsDeleted = 0
    AND (
        s.CompanyId IS NULL
        OR s.CompanyId <> pr.CompanyId
    )
ORDER BY pr.Id;

-- Q4. Tìm sinh viên có nhiều hơn một đơn Approved (bao gồm mọi hàng sinh viên, kể cả xóa mềm).
SELECT
    StudentId,
    COUNT(*) AS ApprovedRequestCount
FROM PlacementRequests
WHERE Status = 'Approved'
GROUP BY StudentId
HAVING COUNT(*) > 1
ORDER BY ApprovedRequestCount DESC, StudentId;

-- Q5. Thống kê trạng thái sinh viên của các đơn Approved thuộc nhóm CompanyId khớp (không xóa mềm).
SELECT
    s.Status AS StudentStatus,
    COUNT(*) AS ApprovedRequestCount
FROM PlacementRequests AS pr
INNER JOIN Students AS s ON s.Id = pr.StudentId
WHERE pr.Status = 'Approved'
    AND s.IsDeleted = 0
    AND s.CompanyId = pr.CompanyId
GROUP BY s.Status
ORDER BY s.Status;

-- Q6. AcceptedCount hiện được tính động trong code, không phải cột JobPositions;
-- báo cáo sinh viên chưa xóa mềm theo từng vị trí cùng số ở trạng thái Accepted/Interning/Completed.
SELECT
    jp.Id AS JobPositionId,
    jp.Title AS JobPositionTitle,
    COUNT(s.Id) AS ActiveStudentCount,
    COUNT(CASE
        WHEN s.Status IN ('Accepted', 'Interning', 'Completed') THEN 1
    END) AS AcceptedStatusStudentCount
FROM JobPositions AS jp
LEFT JOIN Students AS s
    ON s.JobPositionId = jp.Id
    AND s.IsDeleted = 0
GROUP BY jp.Id, jp.Title
ORDER BY jp.Id;
