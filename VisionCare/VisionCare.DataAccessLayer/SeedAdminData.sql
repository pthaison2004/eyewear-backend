INSERT INTO SystemSettings (SettingKey, SettingValue, Description, GroupName, UpdatedAt) VALUES
('SiteName', 'VisionCare - Hệ thống bán lẻ kính mắt', 'Tên website hiển thị chính', 'General', GETDATE()),
('SupportEmail', 'contact@visioncare.vn', 'Email hỗ trợ khách hàng', 'General', GETDATE()),
('Currency', 'VND', 'Định dạng tiền tệ hệ thống', 'Regional', GETDATE()),
('AllowPreOrder', 'true', 'Bật/Tắt tính năng đặt trước', 'Features', GETDATE()),
('SmtpServer', 'smtp.visioncare.vn', 'Máy chủ gửi email', 'Email', GETDATE()),
('SmtpPort', '587', 'Cổng SMTP', 'Email', GETDATE()),
('ShippingInnerCityFee', '25000', 'Phí ship nội thành', 'Shipping', GETDATE()),
('ShippingOuterCityFee', '35000', 'Phí ship ngoại thành', 'Shipping', GETDATE()),
('ShippingFreeThreshold', '2000000', 'Ngưỡng miễn phí ship (VNĐ)', 'Shipping', GETDATE());
