-- Seed document export templates into configurations.
-- ParamValue is copied from document-export-templates.json and stored as JSON text.
-- MySQL 8 syntax.

INSERT INTO configurations (ConfigGroup, ParamKey, ParamValue, Description)
VALUES
('DOCUMENT_TEMPLATE', 'EXPORT_CONTRACT', JSON_OBJECT(
  'templateKey', 'EXPORT_CONTRACT',
  'documentType', 'CONTRACT',
  'displayName', 'Hop dong lao dong',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'contract-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF', 'DOCX'),
  'dataScope', JSON_OBJECT('module', 'EmployeeProfile', 'entity', 'Contract', 'referenceIdField', 'contractId'),
  'placeholders', JSON_ARRAY('company_name','company_address','company_tax_code','director_name','employee_name','employee_code','employee_identity_number','employee_address','department_name','position_name','contract_number','contract_type','basic_salary','insurance_salary','salary_percentage','start_date','end_date','effective_date','created_date'),
  'layoutVersions', JSON_ARRAY(
    JSON_OBJECT(
      'version', 'contract-standard-v1',
      'name', 'Mau tieu chuan',
      'isActive', true,
      'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm 18mm 18mm 18mm'),
      'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#1d4ed8', 'logoUrl', ''),
      'headerHtml', '<div style="text-align:center"><strong>{company_name}</strong><br/><span>{company_address}</span></div>',
      'bodyHtml', '<h1 style="text-align:center">HOP DONG LAO DONG</h1><p>So hop dong: <strong>{contract_number}</strong></p><p>Hom nay, ngay {created_date}, chung toi gom:</p><p><strong>Ben su dung lao dong:</strong> {company_name}, dai dien boi {director_name}.</p><p><strong>Nguoi lao dong:</strong> {employee_name} - Ma NV: {employee_code} - CCCD: {employee_identity_number}.</p><p>Chuc danh: {position_name}, phong ban: {department_name}.</p><p>Loai hop dong: {contract_type}. Thoi han tu {start_date} den {end_date}.</p><p>Luong co ban: {basic_salary}. Luong dong bao hiem: {insurance_salary}. Ty le luong: {salary_percentage}.</p><p>Hop dong co hieu luc tu ngay {effective_date}.</p>',
      'footerHtml', '<table style="width:100%; margin-top:40px; text-align:center"><tr><td>DAI DIEN CONG TY<br/><br/><br/>{director_name}</td><td>NGUOI LAO DONG<br/><br/><br/>{employee_name}</td></tr></table>'
    )
  )
), 'Mau xuat hop dong lao dong'),
('DOCUMENT_TEMPLATE', 'EXPORT_CONTRACT_ADDENDUM', JSON_OBJECT(
  'templateKey', 'EXPORT_CONTRACT_ADDENDUM',
  'documentType', 'CONTRACT_ADDENDUM',
  'displayName', 'Phu luc hop dong',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'addendum-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF', 'DOCX'),
  'dataScope', JSON_OBJECT('module', 'EmployeeProfile', 'entity', 'ContractAddendum', 'referenceIdField', 'addendumId'),
  'placeholders', JSON_ARRAY('company_name','director_name','employee_name','employee_code','contract_number','addendum_number','new_basic_salary','new_insurance_salary','new_end_date','other_changes','effective_date','created_date'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'addendum-standard-v1',
    'name', 'Mau phu luc tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm'),
    'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#1d4ed8', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>{company_name}</strong></div>',
    'bodyHtml', '<h1 style="text-align:center">PHU LUC HOP DONG LAO DONG</h1><p>So phu luc: <strong>{addendum_number}</strong></p><p>Can cu hop dong lao dong so <strong>{contract_number}</strong>, hai ben thong nhat dieu chinh cac noi dung sau:</p><ul><li>Luong co ban moi: {new_basic_salary}</li><li>Luong dong bao hiem moi: {new_insurance_salary}</li><li>Ngay ket thuc hop dong moi: {new_end_date}</li><li>Noi dung khac: {other_changes}</li></ul><p>Phu luc co hieu luc tu ngay {effective_date}.</p>',
    'footerHtml', '<table style="width:100%; margin-top:40px; text-align:center"><tr><td>DAI DIEN CONG TY<br/><br/><br/>{director_name}</td><td>NGUOI LAO DONG<br/><br/><br/>{employee_name}</td></tr></table>'
  ))
), 'Mau xuat phu luc hop dong'),
('DOCUMENT_TEMPLATE', 'EXPORT_LEAVE_REQUEST', JSON_OBJECT(
  'templateKey', 'EXPORT_LEAVE_REQUEST',
  'documentType', 'LEAVE_REQUEST',
  'displayName', 'Don xin nghi phep',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'leave-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF'),
  'dataScope', JSON_OBJECT('module', 'TimeAttendance', 'entity', 'LeaveRequest', 'referenceIdField', 'leaveRequestId'),
  'placeholders', JSON_ARRAY('employee_name','employee_code','department_name','position_name','leave_type','start_date','end_date','days','reason','status','manager_name','director_name','created_date'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'leave-standard-v1',
    'name', 'Mau don nghi phep tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm'),
    'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#2563eb', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>CONG HOA XA HOI CHU NGHIA VIET NAM</strong><br/>Doc lap - Tu do - Hanh phuc</div>',
    'bodyHtml', '<h1 style="text-align:center">DON XIN NGHI PHEP</h1><p>Kinh gui: Ban lanh dao va phong nhan su.</p><p>Toi ten la: <strong>{employee_name}</strong> - Ma NV: {employee_code}</p><p>Phong ban: {department_name} - Chuc danh: {position_name}</p><p>Loai nghi: {leave_type}</p><p>Thoi gian nghi: tu {start_date} den {end_date} ({days} ngay)</p><p>Ly do: {reason}</p><p>Trang thai xu ly: {status}</p>',
    'footerHtml', '<table style="width:100%; margin-top:40px; text-align:center"><tr><td>Truong phong<br/>{manager_name}</td><td>Giam doc<br/>{director_name}</td><td>Nguoi lam don<br/>{employee_name}</td></tr></table>'
  ))
), 'Mau xuat don nghi phep'),
('DOCUMENT_TEMPLATE', 'EXPORT_OVERTIME_REQUEST', JSON_OBJECT(
  'templateKey', 'EXPORT_OVERTIME_REQUEST',
  'documentType', 'OVERTIME_REQUEST',
  'displayName', 'Phieu dang ky tang ca',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'ot-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF'),
  'dataScope', JSON_OBJECT('module', 'TimeAttendance', 'entity', 'OvertimeRequest', 'referenceIdField', 'overtimeRequestId'),
  'placeholders', JSON_ARRAY('employee_name','employee_code','department_name','work_date','start_time','end_time','approved_minutes','actual_ot_minutes','reason','project_code','status','manager_note','hr_note'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'ot-standard-v1',
    'name', 'Mau OT tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm'),
    'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#ea580c', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>PHIEU DANG KY LAM THEM GIO</strong></div>',
    'bodyHtml', '<p>Nhan vien: <strong>{employee_name}</strong> - Ma NV: {employee_code}</p><p>Phong ban: {department_name}</p><p>Ngay lam them: {work_date}</p><p>Thoi gian: {start_time} - {end_time}</p><p>So phut dang ky/duyet: {approved_minutes}</p><p>So phut OT thuc te doi chieu cham cong: {actual_ot_minutes}</p><p>Du an/Ma cong viec: {project_code}</p><p>Ly do: {reason}</p><p>Trang thai: {status}</p><p>Ghi chu quan ly: {manager_note}</p><p>Ghi chu HR: {hr_note}</p>',
    'footerHtml', '<div style="margin-top:40px;text-align:right">Nguoi lap phieu: {employee_name}</div>'
  ))
), 'Mau xuat phieu dang ky OT'),
('DOCUMENT_TEMPLATE', 'EXPORT_PROFILE_UPDATE_REQUEST', JSON_OBJECT(
  'templateKey', 'EXPORT_PROFILE_UPDATE_REQUEST',
  'documentType', 'PROFILE_UPDATE_REQUEST',
  'displayName', 'Phieu yeu cau cap nhat ho so',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'profile-update-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF'),
  'dataScope', JSON_OBJECT('module', 'EmployeeProfile', 'entity', 'ProfileUpdateRequest', 'referenceIdField', 'profileUpdateRequestId'),
  'placeholders', JSON_ARRAY('employee_name','employee_code','department_name','requested_fields','old_values','new_values','status','reject_reason','created_date','reviewed_by','reviewed_date'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'profile-update-standard-v1',
    'name', 'Mau cap nhat ho so tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm'),
    'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#059669', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>PHIEU YEU CAU CAP NHAT HO SO NHAN SU</strong></div>',
    'bodyHtml', '<p>Nhan vien: <strong>{employee_name}</strong> - Ma NV: {employee_code}</p><p>Phong ban: {department_name}</p><p>Ngay gui yeu cau: {created_date}</p><p>Cac truong yeu cau thay doi: {requested_fields}</p><p>Gia tri cu: {old_values}</p><p>Gia tri moi: {new_values}</p><p>Trang thai: {status}</p><p>Ly do tu choi neu co: {reject_reason}</p><p>Nguoi xu ly: {reviewed_by} - Ngay xu ly: {reviewed_date}</p>',
    'footerHtml', '<div style="margin-top:40px;text-align:right">Nguoi yeu cau: {employee_name}</div>'
  ))
), 'Mau xuat phieu yeu cau thay doi thong tin ho so'),
('DOCUMENT_TEMPLATE', 'EXPORT_ONBOARDING_PROFILE', JSON_OBJECT(
  'templateKey', 'EXPORT_ONBOARDING_PROFILE',
  'documentType', 'ONBOARDING_PROFILE',
  'displayName', 'Phieu thiet lap ho so nhan su',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'onboarding-profile-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF'),
  'dataScope', JSON_OBJECT('module', 'EmployeeProfile', 'entity', 'OnboardingRequest', 'referenceIdField', 'onboardingRequestId'),
  'placeholders', JSON_ARRAY('candidate_name','candidate_email','employee_code','employee_name','department_name','position_name','role_name','employee_type','identity_number','phone_number','personal_email','status','created_date','reviewed_by','reviewed_date'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'onboarding-profile-standard-v1',
    'name', 'Mau thiet lap ho so tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm'),
    'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#0891b2', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>PHIEU THIET LAP HO SO NHAN SU</strong></div>',
    'bodyHtml', '<p>Ung vien: <strong>{candidate_name}</strong> - Email: {candidate_email}</p><p>Nhan vien duoc kich hoat: <strong>{employee_name}</strong> - Ma NV: {employee_code}</p><p>Phong ban: {department_name} - Chuc danh: {position_name}</p><p>Quyen he thong: {role_name} - Loai nhan su: {employee_type}</p><p>CCCD: {identity_number}</p><p>Dien thoai: {phone_number} - Email ca nhan: {personal_email}</p><p>Trang thai: {status}</p><p>Ngay gui: {created_date}</p><p>Nguoi xu ly: {reviewed_by} - Ngay xu ly: {reviewed_date}</p>',
    'footerHtml', '<div style="margin-top:40px;text-align:right">Phong nhan su</div>'
  ))
), 'Mau xuat phieu thiet lap ho so nhan su'),
('DOCUMENT_TEMPLATE', 'EXPORT_RECRUITMENT_REQUEST', JSON_OBJECT(
  'templateKey', 'EXPORT_RECRUITMENT_REQUEST',
  'documentType', 'RECRUITMENT_REQUEST',
  'displayName', 'Phieu de xuat tuyen dung',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'recruitment-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF'),
  'dataScope', JSON_OBJECT('module', 'Recruitment', 'entity', 'RecruitmentRequest', 'referenceIdField', 'recruitmentRequestId'),
  'placeholders', JSON_ARRAY('request_code','department_name','position_name','quantity','expected_start_date','reason','description','status','created_by','created_date','hr_reviewer','director_name'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'recruitment-standard-v1',
    'name', 'Mau de xuat tuyen dung tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'portrait', 'margin', '20mm'),
    'theme', JSON_OBJECT('fontFamily', 'Times New Roman', 'fontSize', '12pt', 'primaryColor', '#111827', 'accentColor', '#7c3aed', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>PHIEU DE XUAT NHU CAU TUYEN DUNG</strong></div>',
    'bodyHtml', '<p>Ma de xuat: {request_code}</p><p>Phong ban: {department_name}</p><p>Vi tri can tuyen: {position_name}</p><p>So luong: {quantity}</p><p>Ngay can nhan su du kien: {expected_start_date}</p><p>Ly do tuyen dung: {reason}</p><p>Mo ta yeu cau: {description}</p><p>Trang thai: {status}</p><p>Nguoi tao: {created_by} - Ngay tao: {created_date}</p>',
    'footerHtml', '<table style="width:100%; margin-top:40px; text-align:center"><tr><td>HR<br/>{hr_reviewer}</td><td>Giam doc<br/>{director_name}</td><td>Nguoi de xuat<br/>{created_by}</td></tr></table>'
  ))
), 'Mau xuat phieu de xuat tuyen dung'),
('DOCUMENT_TEMPLATE', 'EXPORT_KPI_REVIEW', JSON_OBJECT(
  'templateKey', 'EXPORT_KPI_REVIEW',
  'documentType', 'KPI_REVIEW',
  'displayName', 'Bao cao danh gia KPI',
  'defaultOutput', 'PDF',
  'activeLayoutVersion', 'kpi-review-standard-v1',
  'allowedOutputs', JSON_ARRAY('PDF', 'XLSX'),
  'dataScope', JSON_OBJECT('module', 'TasksTraining', 'entity', 'PerformanceReview', 'referenceIdField', 'performanceReviewId'),
  'placeholders', JSON_ARRAY('employee_name','employee_code','department_name','position_name','period','total_weight','total_penalty_points','total_score','final_rating','final_comment','kpi_detail_rows','reviewer_name','finalized_date'),
  'layoutVersions', JSON_ARRAY(JSON_OBJECT(
    'version', 'kpi-review-standard-v1',
    'name', 'Mau bao cao KPI tieu chuan',
    'isActive', true,
    'page', JSON_OBJECT('size', 'A4', 'orientation', 'landscape', 'margin', '14mm'),
    'theme', JSON_OBJECT('fontFamily', 'Arial', 'fontSize', '10pt', 'primaryColor', '#111827', 'accentColor', '#16a34a', 'logoUrl', ''),
    'headerHtml', '<div style="text-align:center"><strong>BAO CAO DANH GIA KPI</strong></div>',
    'bodyHtml', '<p>Nhan vien: <strong>{employee_name}</strong> - Ma NV: {employee_code}</p><p>Phong ban: {department_name} - Chuc danh: {position_name}</p><p>Ky danh gia: {period}</p><p>Tong trong so: {total_weight} - Tong diem tru: {total_penalty_points} - Diem sau tru: {total_score}</p><p>Xep loai: {final_rating}</p><table style="width:100%;border-collapse:collapse" border="1"><thead><tr><th>Ma KPI</th><th>Chi tieu</th><th>Trong so</th><th>Diem tru</th><th>Ly do phat</th><th>Diem cuoi</th></tr></thead><tbody>{kpi_detail_rows}</tbody></table><p>Nhan xet: {final_comment}</p>',
    'footerHtml', '<div style="margin-top:32px;text-align:right">Nguoi danh gia: {reviewer_name}<br/>Ngay chot: {finalized_date}</div>'
  ))
), 'Mau xuat bao cao danh gia KPI')
ON DUPLICATE KEY UPDATE
  ParamValue = VALUES(ParamValue),
  Description = VALUES(Description);
