document.addEventListener('cinego:cityChanged', function (e) {
    // Page load par bajne wali ghanti ko ignore karo
    if (e.detail.source !== 'user') return;

    // User ne city badli hai, to us city ke theatres ki list pe bhej do
    window.location.href = '/Theatres?cityId=' + e.detail.cityId;
});