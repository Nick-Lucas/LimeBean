param ([string]$build_number, [string]$tag)

$meta_version_numeric = "0.0"

if ($tag -match '^v?(([.\d]+)[\w-]*)$')  {
    # match version in the format: v1.1.1
    # build script passes a tag on a commit to this script which must follow this format
    $meta_version_full = $matches[1]
    $meta_version_numeric = $matches[2]
} elseif ($build_number) {
    # Otherwise fall back on CI build number
    $meta_version_full = "$meta_version_numeric-ci-build$build_number"
} else {
    # Otherwise cry and accept we don't have a version number
    $meta_version_full = $meta_version_numeric
}

$meta_description = "Hybrid-ORM for .Net"
$meta_author = "Nick Lucas"
$meta_copyright = "Copyright (c) 2014-2016 Aleksey Martynov, 2016-$(get-date -format yyyy) $meta_author"
$meta_project_url = "https://nick-lucas.github.io/LimeBean/"
$meta_license_url = "https://raw.githubusercontent.com/Nick-Lucas/LimeBean/master/LICENSE.txt"

("LimeBean\AssemblyInfo.cs", "LimeBean.NetCore\LimeBean\project.json") | %{
    $path = "$PSScriptRoot\$_"

    (Get-Content $path) | %{
        $_  -replace '(AssemblyVersion.+?")[^"]+', "`${1}$meta_version_numeric" `
            -replace '("version":.+?")[^"]+', "`${1}$meta_version_full" `
            -replace '%lime_description%', $meta_description `
            -replace '%lime_author%', $meta_author `
            -replace '%lime_copyright%', $meta_copyright `
            -replace '%lime_project_url%', $meta_project_url `
            -replace '%lime_license_url%', $meta_license_url
    } | Set-Content $path
}
