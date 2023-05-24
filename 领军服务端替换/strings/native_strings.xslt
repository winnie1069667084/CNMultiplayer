<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
  <xsl:template match="string[@id='str_taunt_name.MpHelp']"/>
  <xsl:template match="string[@id='str_taunt_name.MpSpot']"/>
  <xsl:template match="string[@id='str_taunt_name.MpThanks']"/>
  <xsl:template match="string[@id='str_taunt_name.MpSorry']"/>
  <xsl:template match="string[@id='str_taunt_name.MpAffirmative']"/>
  <xsl:template match="string[@id='str_taunt_name.MpNegative']"/>
  <xsl:template match="string[@id='str_taunt_name.MpRegroup']"/>
  <xsl:template match="string[@id='str_taunt_name.MpDefend']"/>
  <xsl:template match="string[@id='str_taunt_name.MpAttack']"/>
  <xsl:template match="string[@id='str_multiplayer_scene_name.mp_siege_map_004_rs']"/>
</xsl:stylesheet>