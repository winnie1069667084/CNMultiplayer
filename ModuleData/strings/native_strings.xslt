<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output omit-xml-declaration="yes"/>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
  <xsl:template match="string[@id='str_multiplayer_scene_name.mp_siege_map_004_rs']"/>

  <xsl:template match="string[@id='str_gold_gain_first_assist']"/>
  <xsl:template match="string[@id='str_gold_gain_second_assist']"/>
  <xsl:template match="string[@id='str_gold_gain_third_assist']"/>
  <xsl:template match="string[@id='str_gold_gain_first_ranged_kill']"/>
  <xsl:template match="string[@id='str_gold_gain_first_melee_kill']"/>
  <xsl:template match="string[@id='str_gold_gain_fifth_kill']"/>
  <xsl:template match="string[@id='str_gold_gain_tenth_kill']"/>
  <xsl:template match="string[@id='str_gold_gain_default_kill']"/>
  <xsl:template match="string[@id='str_gold_gain_default_assist']"/>
  <xsl:template match="string[@id='str_gold_gain_objective_completed']"/>
  <xsl:template match="string[@id='str_gold_gain_objective_destroyed']"/>
  <xsl:template match="string[@id='str_gold_gain_perk_bonus']"/>
</xsl:stylesheet>